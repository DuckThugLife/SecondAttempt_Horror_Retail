using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Vivox;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public bool IsHost => _currentSession?.Host == AuthenticationService.Instance.PlayerId;

    public event Action<ISession> OnSessionCreated;
    public event Action<ISession> OnSessionJoined;
    public event Action<ISession> OnSessionChanged;
    public event Action<bool> OnBusyChanged;
    public event Action OnHostDisconnected;

    private ISession _currentSession;
    private bool _isBusy;

    private bool _vivoxInitialized = false;
    private bool _voiceActive = false;

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
        _ = EvaluateVoiceStateAsync();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"OnClientDisconnected called - ClientId: {clientId}, IsHost: {IsHost}");

        if (!IsHost)
        {
            // Force UI cleanup on disconnect
            if (PlayerStateMachine.LocalInstance != null)
            {
                var currentState = PlayerStateMachine.LocalInstance.GetCurrentState();
                if (currentState is BaseUIState)
                {
                    Debug.Log("Disconnected - forcing exit from UI state");
                    PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
                }
            }

            Debug.Log("Disconnected from host - creating new session");
            OnHostDisconnected?.Invoke();
            _ = HandleHostDisconnectAsync();
        }

        _ = EvaluateVoiceStateAsync();
    }

    #endregion

    #region Voice

    private async Task EvaluateVoiceStateAsync()
    {
        if (NetworkManager.Singleton == null || _currentSession == null)
            return;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        if (playerCount >= 2 && !_voiceActive)
        {
            await SetupVoiceChat(_currentSession);
            return;
        }

        if (playerCount < 2 && _voiceActive)
        {
            try
            {
                await VivoxService.Instance.LeaveAllChannelsAsync();
                _voiceActive = false;
                Debug.Log("Voice chat stopped (solo lobby)");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Voice shutdown failed: {e.Message}");
            }
        }
    }

    private async Task SetupVoiceChat(ISession session)
    {
        try
        {
            if (_voiceActive)
                return;

            if (!_vivoxInitialized)
            {
                await VivoxService.Instance.InitializeAsync();
                _vivoxInitialized = true;
                Debug.Log("Vivox initialized successfully");
                await Task.Delay(500);
            }

            if (!VivoxService.Instance.IsLoggedIn)
            {
                Debug.Log("Logging in to Vivox...");
                await VivoxService.Instance.LoginAsync();
                await Task.Delay(500);
            }

            var channelName = session.Code;
            Debug.Log($"Joining Vivox channel: {channelName}");

            await VivoxService.Instance.JoinGroupChannelAsync(
                channelName,
                ChatCapability.AudioOnly
            );

            _voiceActive = true;
            Debug.Log($"Successfully joined voice channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Voice chat failed: {e.Message}");
            _voiceActive = false;
            await ResetVivoxAsync();
        }
    }

    private async Task ResetVivoxAsync()
    {
        try
        {
            if (VivoxService.Instance != null)
            {
                try { await VivoxService.Instance.LeaveAllChannelsAsync(); } catch { }
                if (VivoxService.Instance.IsLoggedIn)
                    try { await VivoxService.Instance.LogoutAsync(); } catch { }
            }
        }
        finally
        {
            _vivoxInitialized = false;
            _voiceActive = false;
        }
    }

    #endregion

    #region Helper Methods

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
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        if (networkManager.IsListening)
            networkManager.Shutdown();

        var shutdownTask = Task.Run(async () =>
        {
            while (networkManager != null && networkManager.IsListening)
                await Task.Delay(50);
        });

        await Task.WhenAny(shutdownTask, Task.Delay(3000));
    }

    #endregion

    #region Session Management

    private async Task<ISession> CreateAndHostSessionAsync()
    {
        await WaitForNetworkShutdownAsync();
        await ResetVivoxAsync();

        var options = new SessionOptions { MaxPlayers = 4 }.WithRelayNetwork();
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);

        SetCurrentSession(session);
        OnSessionCreated?.Invoke(session);
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
            await ResetVivoxAsync();

            // attempt the join - it will throw if invalid
            var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            SetCurrentSession(session);
            OnSessionJoined?.Invoke(session);
        }
        catch (SessionException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("invalid"))
        {
            Debug.Log($"Session with code {code} doesn't exist");

            // Immediately fall back to lobby and host
            SceneManager.LoadScene("LobbyScene");
            await CreateAndHostSessionAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to join session: {e.Message}");
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
        if (_currentSession == null)
            return;

        SetBusy(true);

        try
        {
            await ResetVivoxAsync();
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

        // Force state cleanup on host disconnect
        if (PlayerStateMachine.LocalInstance != null)
        {
            var currentState = PlayerStateMachine.LocalInstance.GetCurrentState();

            // If we're in any UI state (LobbyMenuState is a BaseUIState), force back to clean LobbyState
            if (currentState is BaseUIState)
            {
                Debug.Log("Host disconnected - forcing exit from UI state");
                PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
            }
        }

        await ResetVivoxAsync();
        SetCurrentSession(null);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await WaitForNetworkShutdownAsync();
        }

        // Make sure we're in the lobby scene
        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            SceneManager.LoadScene("LobbyScene");
            await WaitForSceneLoadAsync("LobbyScene");
        }

        SetBusy(false);
        await CreateAndHostSessionAsync();
    }

    private async Task WaitForSceneLoadAsync(string sceneName, float timeoutSeconds = 5f)
    {
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            await Task.Delay(100);
            elapsed += 0.1f;
        }

        Debug.LogWarning($"Scene load timeout after {timeoutSeconds}s");
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

    #region Internal

    private void SetBusy(bool value)
    {
        _isBusy = value;
        OnBusyChanged?.Invoke(value);
    }

    #endregion
}