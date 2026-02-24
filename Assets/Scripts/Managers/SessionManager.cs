using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    public event Action<ISession> OnSessionCreated;
    public event Action<bool> OnBusyChanged;

    private ISession _currentSession;
    private bool _isBusy;

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
    }

    private void OnDisable()
    {
        NetBootstrap.OnServicesInitialized -= HandleServicesReady;
    }

    #endregion

    #region Initialization

    private async void HandleServicesReady()
    {
        SceneManager.LoadScene("LobbyScene");
        await HostSessionAsync();
    }

    #endregion

    #region Solo Mode

    public void StartSoloGame()
    {
        if (_isBusy || NetworkManager.Singleton.IsListening)
            return;

        NetworkManager.Singleton.StartHost();
        StartGameForAllPlayers();
    }

    #endregion

    #region Hosting

    public async Task HostSessionAsync(int maxPlayers = 4)
    {
        if (_isBusy || !NetBootstrap.IsInitialized)
            return;

        SetBusy(true);

        try
        {
            await CleanupNetworkSessionAsync();
            _currentSession = await CreateNewSessionAsync(maxPlayers);
            StartHostIfNeeded();

            UIManager.Instance.UpdateSessionCode(_currentSession);
            OnSessionCreated?.Invoke(_currentSession);
        }
        catch (Exception e)
        {
            Debug.LogError($"Host Session Failed: {e.Message}");
            _currentSession = null;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CleanupNetworkSessionAsync()
    {
        if (NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        await Task.CompletedTask;
    }

    private async Task<ISession> CreateNewSessionAsync(int maxPlayers)
    {
        var options = new SessionOptions
        {
            MaxPlayers = maxPlayers
        }.WithRelayNetwork();

        return await MultiplayerService.Instance.CreateSessionAsync(options);
    }

    private void StartHostIfNeeded()
    {
        if (!NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartHost();
    }

    #endregion

    #region Joining

    public async Task JoinSessionAsync(string newCode)
    {
        if (!CanAttemptJoin(newCode, out string validationError))
        {
            Debug.LogError(validationError);
            return;
        }

        SetBusy(true);

        string oldCode = _currentSession?.Code;
        bool hadPreviousSession = _currentSession != null;

        await LeaveCurrentSessionIfAny();

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

    private bool CanAttemptJoin(string code, out string error)
    {
        error = null;

        if (_isBusy || string.IsNullOrWhiteSpace(code))
        {
            error = "Cannot join: Session manager is busy or code is empty";
            return false;
        }

        if (!Regex.IsMatch(code, @"^[A-Z0-9]{6}$"))
        {
            error = $"Invalid code format: {code}";
            return false;
        }

        return true;
    }

    private async Task LeaveCurrentSessionIfAny()
    {
        if (_currentSession == null)
            return;

        try
        {
            await _currentSession.LeaveAsync();
        }
        catch (Exception leaveEx)
        {
            Debug.LogWarning($"Failed to leave session: {leaveEx.Message}");
        }

        _currentSession = null;

        if (NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    private async Task AttemptJoinNewSessionAsync(string newCode)
    {
        _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(newCode);

        if (!NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.StartClient();

        Debug.Log("Successfully joined new session!");
    }

    private async Task HandleJoinFailureAsync(Exception joinEx, string newCode, string oldCode, bool hadPreviousSession)
    {
        Debug.LogWarning($"Failed to join new session ({newCode}): {joinEx.Message}");

        await Task.Delay(1000);

        if (hadPreviousSession && await TryRejoinOldSessionAsync(oldCode))
            return;

        await HostSessionAsync();
    }

    private async Task<bool> TryRejoinOldSessionAsync(string oldCode)
    {
        if (string.IsNullOrEmpty(oldCode))
            return false;

        try
        {
            _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(oldCode);

            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartClient();

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

    #region Game Flow

    public void StartGameForAllPlayers()
    {
        if (!NetworkManager.Singleton.IsHost)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public async Task LeaveSessionAsync()
    {
        SetBusy(true);

        try
        {
            if (_currentSession != null)
                await _currentSession.LeaveAsync();

            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();
        }
        finally
        {
            _currentSession = null;
            SetBusy(false);
            SceneManager.LoadScene("LobbyScene");
        }
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