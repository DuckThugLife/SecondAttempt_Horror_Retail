using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    public event Action<ISession> OnSessionCreated;
    public event Action<ISession> OnSessionJoined;
    public event Action<bool> OnBusyChanged;
    public event Action OnHostDisconnected;

    private ISession _currentSession;
    private bool _isBusy;

    // Message name constant
    private const string HostLeavingMessageName = "HostLeaving";

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        NetBootstrap.OnServicesInitialized += HandleServicesReady;

        // Subscribe to network disconnect events
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("Subscribing to disconnect events");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    HostLeavingMessageName,
                    OnHostLeavingMessage
                );
            }
        }
        else
        {
            Debug.LogWarning("NetworkManager.Singleton is null in OnEnable - will try again later");
            Invoke(nameof(TrySubscribeToDisconnect), 0.5f);
        }

        // If already initialized, handle it immediately
        if (NetBootstrap.IsInitialized)
        {
            HandleServicesReady();
        }
    }

    private void TrySubscribeToDisconnect()
    {
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("Now subscribing to disconnect events");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    HostLeavingMessageName,
                    OnHostLeavingMessage
                );
            }
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton still null - disconnect handling won't work");
        }
    }

    private void OnDisable()
    {
        NetBootstrap.OnServicesInitialized -= HandleServicesReady;

        // Safely unsubscribe
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            // Unregister message handler by setting to null
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                HostLeavingMessageName,
                null
            );
        }

        CancelInvoke(nameof(TrySubscribeToDisconnect));
    }

    #endregion

    #region Initialization

    private async void HandleServicesReady()
    {
        // Small delay to ensure scene is loaded
        await Task.Delay(100);

        SceneManager.LoadScene("LobbyScene");
        await HostSessionAsync();
    }

    #endregion

    #region Hosting Methods

    public async Task HostSessionAsync(int maxPlayers = 4)
    {
        Debug.Log($"HostSessionAsync STARTED - Busy: {_isBusy}, Initialized: {NetBootstrap.IsInitialized}");

        if (_isBusy || !NetBootstrap.IsInitialized)
        {
            Debug.Log($"HostSessionAsync skipped - Busy: {_isBusy}, Initialized: {NetBootstrap.IsInitialized}");
            return;
        }

        SetBusy(true);
        Debug.Log("HostSessionAsync - SetBusy(true) completed");

        try
        {
            Debug.Log("HostSessionAsync - Cleaning up network session...");
            await CleanupNetworkSessionAsync();
            Debug.Log("HostSessionAsync - Cleanup complete");

            Debug.Log("HostSessionAsync - Creating new session...");
            _currentSession = await CreateNewSessionAsync(maxPlayers);
            Debug.Log($"HostSessionAsync - Session created with code: {_currentSession?.Code}");

            Debug.Log("HostSessionAsync - Starting host...");
            StartHostIfNeeded();
            Debug.Log("HostSessionAsync - Host started");

            Debug.Log("HostSessionAsync - Invoking OnSessionCreated event");
            OnSessionCreated?.Invoke(_currentSession);

            Debug.Log("HostSessionAsync - COMPLETE - Player should spawn");
        }
        catch (Exception e)
        {
            Debug.LogError($"HostSessionAsync FAILED: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            _currentSession = null;
        }
        finally
        {
            SetBusy(false);
            Debug.Log("HostSessionAsync - SetBusy(false) completed");
        }
    }

    private async Task CleanupNetworkSessionAsync()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        await Task.CompletedTask;
    }

    private async Task<ISession> CreateNewSessionAsync(int maxPlayers)
    {
        Debug.Log("CreateNewSessionAsync - Creating session options");
        var options = new SessionOptions
        {
            MaxPlayers = maxPlayers
        }.WithRelayNetwork();

        Debug.Log("CreateNewSessionAsync - Calling MultiplayerService.Instance.CreateSessionAsync...");
        try
        {
            var session = await MultiplayerService.Instance.CreateSessionAsync(options);
            Debug.Log($"CreateNewSessionAsync - SUCCESS: Session created with code: {session.Code}");
            return session;
        }
        catch (Exception e)
        {
            Debug.LogError($"CreateNewSessionAsync - FAILED: {e.Message}");
            throw;
        }
    }

    private void StartHostIfNeeded()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartHost();
    }

    #endregion

    #region Joining Methods

    public async Task JoinSessionAsync(string newCode)
    {
        if (_isBusy)
        {
            Debug.LogError("Cannot join: Session manager is busy");
            return;
        }

        if (string.IsNullOrWhiteSpace(newCode))
        {
            Debug.LogError("Cannot join: Code is empty");
            return;
        }

        SetBusy(true);

        string oldCode = _currentSession?.Code;
        bool hadPreviousSession = _currentSession != null;

        await LeaveCurrentSessionIfAny();

        // Workaround: Force re-authentication to clear Unity lobby service cached state
        Debug.Log("Workaround: Signing out and back in to clear lobby state");
        try
        {
            AuthenticationService.Instance.SignOut();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Re-authentication complete");
        }
        catch (Exception authEx)
        {
            Debug.LogError($"Re-authentication failed: {authEx.Message}");
        }

        // Small delay to ensure cleanup
        await Task.Delay(500);

        try
        {
            await AttemptJoinNewSessionAsync(newCode);
        }
        catch (Exception joinEx)
        {
            await HandleJoinFailureAsync(joinEx, newCode, oldCode, hadPreviousSession);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AttemptJoinNewSessionAsync(string newCode)
    {
        if (!Regex.IsMatch(newCode, @"^[A-Z0-9]{6}$"))
        {
            throw new Exception("Invalid code format");
        }

        Debug.Log($"Attempting to join session: {newCode}");
        _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(newCode);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartClient();

        OnSessionJoined?.Invoke(_currentSession);

        Debug.Log("Successfully joined new session!");
    }

    private async Task HandleJoinFailureAsync(Exception joinEx, string newCode, string oldCode, bool hadPreviousSession)
    {
        Debug.LogWarning($"Failed to join new session ({newCode}): {joinEx.Message}");

        await Task.Delay(1000);

        if (hadPreviousSession && await TryRejoinOldSessionAsync(oldCode))
            return;

        // Reset busy flag before hosting fallback
        SetBusy(false);

        Debug.Log("Falling back to hosting new session");
        await HostSessionAsync();
    }

    private async Task<bool> TryRejoinOldSessionAsync(string oldCode)
    {
        if (string.IsNullOrEmpty(oldCode))
            return false;

        try
        {
            Debug.Log($"Attempting to rejoin previous session: {oldCode}");
            _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(oldCode);

            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartClient();

            OnSessionJoined?.Invoke(_currentSession);

            Debug.Log("Rejoined previous session.");
            return true;
        }
        catch (Exception oldJoinEx)
        {
            Debug.LogWarning($"Failed to rejoin previous session ({oldCode}): {oldJoinEx.Message}");
            return false;
        }
    }

    #endregion

    #region Leave Methods

    public async Task LeaveSessionAsync()
    {
        SetBusy(true);

        try
        {
            if (IsHost)
            {
                Debug.Log("Host is leaving - sending notification to clients");

                // Send message to all clients that host is leaving
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    var writer = new FastBufferWriter(4, Allocator.Temp);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                        HostLeavingMessageName,
                        writer
                    );
                    writer.Dispose();

                    // Small delay for message to send
                    await Task.Delay(500);
                }

                await EndSessionForAllAsync();
            }
            else
            {
                Debug.Log("Client is leaving session");
                await LeaveCurrentSessionIfAny();
            }
        }
        finally
        {
            _currentSession = null;
            SetBusy(false);
            SceneManager.LoadScene("LobbyScene");

            // Host a new session after returning to lobby
            await HostSessionAsync();
        }
    }

    private async Task LeaveCurrentSessionIfAny()
    {
        if (_currentSession == null)
            return;

        try
        {
            Debug.Log($"Leaving session: {_currentSession.Code}");
            await _currentSession.LeaveAsync();
            Debug.Log("LeaveAsync completed");
        }
        catch (Exception leaveEx)
        {
            Debug.LogWarning($"Failed to leave session: {leaveEx.Message}");
        }

        _currentSession = null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Network shutdown after leave");
        }
    }

    private async Task EndSessionForAllAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error ending session: {e.Message}");
            }
        }

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
    }

    #endregion

    #region Disconnect Handling

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"OnClientDisconnected called - ClientId: {clientId}, IsHost: {IsHost}");

        // If we're not the host and we got disconnected (any clientId means host left)
        if (!IsHost)
        {
            Debug.Log("Disconnected from host - falling back to new host session");
            OnHostDisconnected?.Invoke();
            _ = HandleHostDisconnectAsync();
        }
    }

    private void OnHostLeavingMessage(ulong clientId, FastBufferReader reader)
    {
        Debug.Log("Received host leaving notification");
        OnHostDisconnected?.Invoke();
        _ = HandleHostDisconnectAsync();
    }

    private async Task HandleHostDisconnectAsync()
    {
        Debug.Log("Handling host disconnect - START");
        SetBusy(true);

        try
        {
            // Clean up old session
            _currentSession = null;
            Debug.Log("Session cleared");

            // Shutdown network
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Network shutdown after host disconnect");
            }

            // Only load scene if not already in lobby
            if (SceneManager.GetActiveScene().name != "LobbyScene")
            {
                Debug.Log($"Loading LobbyScene. Current scene: {SceneManager.GetActiveScene().name}");
                SceneManager.LoadScene("LobbyScene");
                await Task.Delay(1000);
            }

            // Set busy to false BEFORE hosting
            SetBusy(false);

            // Host a new session
            Debug.Log("Hosting new session after host disconnect");
            await HostSessionAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling host disconnect: {e.Message}");
        }
        finally
        {
            SetBusy(false);
            Debug.Log("Handling host disconnect - END");
        }
    }

    #endregion

    #region Game Flow

    public void StartGameForAllPlayers()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void StartSoloGame()
    {
        if (_isBusy || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening))
            return;

        NetworkManager.Singleton.StartHost();
        StartGameForAllPlayers();
    }

    #endregion

    #region Internal Helpers

    private void SetBusy(bool value)
    {
        _isBusy = value;
        OnBusyChanged?.Invoke(value);
    }

    #endregion
}