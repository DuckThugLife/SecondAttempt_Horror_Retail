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
        Bootstrapper.OnServicesInitialized += HandleServicesReady;

        if (Bootstrapper.ServicesInitialized)
            HandleServicesReady();

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
        Bootstrapper.OnServicesInitialized -= HandleServicesReady;

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
        Debug.Log("Services ready, hosting session...");
        await HostSessionAsync();
    }

    #endregion

    #region Hosting Methods

    public async Task HostSessionAsync(int maxPlayers = 4)
    {
        Debug.Log($"HostSessionAsync STARTED - Busy: {_isBusy}, Initialized: {Bootstrapper.ServicesInitialized}");

        if (_isBusy || !Bootstrapper.ServicesInitialized)
        {
            Debug.Log($"HostSessionAsync skipped - Busy: {_isBusy}, Initialized: {Bootstrapper.ServicesInitialized}");
            return;
        }

        SetBusy(true);
        Debug.Log("HostSessionAsync - SetBusy(true) completed");

        // Don't catch here - let exceptions bubble up to retry logic
        Debug.Log("HostSessionAsync - Cleaning up network session...");
        await CleanupNetworkSessionAsync();
        Debug.Log("HostSessionAsync - Cleanup complete");

        Debug.Log("HostSessionAsync - Creating new session...");
        _currentSession = await CreateNewSessionAsync(maxPlayers); // This will throw on rate limit
        Debug.Log($"HostSessionAsync - Session created with code: {_currentSession?.Code}");

        Debug.Log("HostSessionAsync - Starting host...");
        StartHostIfNeeded();
        Debug.Log("HostSessionAsync - Host started");

        Debug.Log("HostSessionAsync - Invoking OnSessionCreated event");
        OnSessionCreated?.Invoke(_currentSession);

        Debug.Log("HostSessionAsync - COMPLETE - Player should spawn");

        SetBusy(false);
        Debug.Log("HostSessionAsync - SetBusy(false) completed");
    }

    private async Task<bool> TryHostWithRetryAsync(int maxRetries = 5)
    {
        int retryCount = 0;
        int baseDelay = 1000; // Start with 1 second

        while (retryCount < maxRetries)
        {
            try
            {
                // Ensure we're not busy before trying
                SetBusy(false);

                await HostSessionAsync();
                return true; // Success!
            }
            catch (Exception e) when (
                e.Message.Contains("Too Many Requests") ||
                e.Message.Contains("Rate") ||
                e.Message.Contains("Unable to read data") ||
                e.Message.Contains("timeout") ||
                e.Message.Contains("connection")
            )
            {
                retryCount++;

                // Exponential backoff: 1s, 2s, 4s, 8s, 16s
                int delayMs = baseDelay * (int)Math.Pow(2, retryCount - 1);
                Debug.Log($"Network error. Retry {retryCount}/{maxRetries} in {delayMs / 1000}s");

                // Make sure we're not busy for the next attempt
                SetBusy(false);

                await Task.Delay(delayMs);
            }
            catch (Exception e)
            {
                // Different error - fail immediately
                Debug.LogError($"Host failed: {e.Message}");
                SetBusy(false);
                return false;
            }
        }

        Debug.LogError("Max retries exceeded - giving up");
        SetBusy(false);
        return false;
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
            await HandleJoinFailureAsync(joinEx, newCode);
        }
        finally
        {
            SetBusy(false);
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

    private async Task HandleJoinFailureAsync(Exception joinEx, string newCode)
    {
        Debug.LogWarning($"Failed to join new session ({newCode}): {joinEx.Message}");

        await Task.Delay(1000);

        // Skip old lobby rejoin - go straight to hosting new session
        // Reset busy flag before hosting fallback
        SetBusy(false);

        Debug.Log("Falling back to hosting new session");
        await TryHostWithRetryAsync(); // Use retry logic here too
    }

    #endregion

    #region Leave Methods

    public async Task LeaveSessionAsync()
    {
        SetBusy(true);

        // Force exit UI state before leaving
        if (PlayerStateMachine.LocalInstance != null &&
            PlayerStateMachine.LocalInstance.GetCurrentState() is BaseUIState)
        {
            PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
        }

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

            // Host a new session after returning to lobby with retry logic
            await TryHostWithRetryAsync();
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
            // Force exit UI state
            if (PlayerStateMachine.LocalInstance != null &&
                PlayerStateMachine.LocalInstance.GetCurrentState() is BaseUIState)
            {
                PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
            }

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

            // Host a new session with retry logic
            Debug.Log("Hosting new session after host disconnect");
            await TryHostWithRetryAsync();
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