using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    public bool IsHost => _currentSession?.Host == AuthenticationService.Instance.PlayerId;
    public ISession CurrentSession => _currentSession;

    public event Action<ISession> OnSessionChanged;
    public event Action<bool> OnBusyChanged;
    public event Action OnHostDisconnected;

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
        Bootstrapper.OnServicesInitialized += HandleServicesReady;

        if (Bootstrapper.ServicesInitialized)
            HandleServicesReady();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null in OnEnable - check boot order");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        Bootstrapper.OnServicesInitialized -= HandleServicesReady;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    #endregion

    #region Initialization

    private async void HandleServicesReady()
    {
        Debug.Log("Services ready, hosting session...");
        await CreateAndHostSessionAsync();
    }

    #endregion

    #region Connection Events

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");

        if (!IsHost)
        {
            if (PlayerStateMachine.LocalInstance != null)
            {
                var currentState = PlayerStateMachine.LocalInstance.GetCurrentState();
                if (currentState is BaseUIState)
                {
                    PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
                }
            }

            OnHostDisconnected?.Invoke();
            _ = HandleHostDisconnectAsync();
        }
    }

    #endregion

    #region Helpers

    private void SetCurrentSession(ISession newSession)
    {
        if (_currentSession != null)
            _currentSession.SessionHostChanged -= HandleSessionHostChanged;

        _currentSession = newSession;

        if (_currentSession != null)
            _currentSession.SessionHostChanged += HandleSessionHostChanged;

        OnSessionChanged?.Invoke(_currentSession);
    }

    private async Task WaitForNetworkShutdownAsync()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsListening)
            nm.Shutdown();

        while (nm != null && nm.IsListening)
            await Task.Delay(50);
    }

    #endregion

    #region Session Management

    private async Task<ISession> CreateAndHostSessionAsync()
    {
        await WaitForNetworkShutdownAsync();

        var options = new SessionOptions { MaxPlayers = 4 }.WithRelayNetwork();
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);

        SetCurrentSession(session);
        return session;
    }

    public async Task JoinSessionAsync(string code)
    {
        if (_isBusy || string.IsNullOrWhiteSpace(code))
            return;

        SetBusy(true);

        try
        {
            await WaitForNetworkShutdownAsync();

            // Clear old session data before joining new one
            if (_currentSession != null)
            {
                Debug.Log($"Clearing old session: {_currentSession.Code}");
                _currentSession.SessionHostChanged -= HandleSessionHostChanged;
                _currentSession = null;
            }

            // Reset voice state before joining new session
            if (VoiceManager.Instance != null)
            {
                await VoiceManager.Instance.ResetVoice();
            }

            Debug.Log($"Attempting to join session: {code}");
            var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            SetCurrentSession(session);
            Debug.Log($"Successfully joined session: {session.Code}");
        }
        catch (SessionException ex)
        {
            Debug.Log($"Session with code {code} doesn't exist: {ex.Message}");
            SceneManager.LoadScene("LobbyScene");
            await CreateAndHostSessionAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Join failed: {e.Message}");
            SceneManager.LoadScene("LobbyScene");
            await CreateAndHostSessionAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task LeaveSessionAsync()
    {
        if (_currentSession == null) return;

        SetBusy(true);

        try
        {
            // Reset voice before leaving
            if (VoiceManager.Instance != null)
            {
                await VoiceManager.Instance.ResetVoice();
            }

            await _currentSession.LeaveAsync();

            SetCurrentSession(null);

            NetworkManager.Singleton?.Shutdown();

            SceneManager.LoadScene("LobbyScene");
            await CreateAndHostSessionAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task HandleHostDisconnectAsync()
    {
        SetBusy(true);

        if (PlayerStateMachine.LocalInstance != null)
        {
            var state = PlayerStateMachine.LocalInstance.GetCurrentState();
            if (state is BaseUIState)
                PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
        }

        // Reset voice on host disconnect
        if (VoiceManager.Instance != null)
        {
            await VoiceManager.Instance.ResetVoice();
        }

        SetCurrentSession(null);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await WaitForNetworkShutdownAsync();
        }

        if (SceneManager.GetActiveScene().name != "LobbyScene")
            SceneManager.LoadScene("LobbyScene");

        SetBusy(false);
        await CreateAndHostSessionAsync();
    }

    private void HandleSessionHostChanged(string newHostId)
    {
        if (newHostId != AuthenticationService.Instance.PlayerId)
        {
            OnHostDisconnected?.Invoke();
            _ = HandleHostDisconnectAsync();
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
        if (_isBusy || NetworkManager.Singleton?.IsListening == true)
            return;

        NetworkManager.Singleton.StartHost();
        StartGameForAllPlayers();
    }

    #endregion

    private void SetBusy(bool value)
    {
        _isBusy = value;
        OnBusyChanged?.Invoke(value);
    }
}