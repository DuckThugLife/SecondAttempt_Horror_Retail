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
    public event Action<ISession> OnSessionChanged;  // Added this back
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
        await HostSessionAsync();
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
            if (PlayerStateMachine.LocalInstance != null &&
                PlayerStateMachine.LocalInstance.GetCurrentState() is BaseUIState)
            {
                PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
            }

            Debug.Log("Disconnected from host - falling back to new host session");
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
            }

            var channelName = session.Code;

            await VivoxService.Instance.JoinGroupChannelAsync(
                channelName,
                ChatCapability.AudioOnly
            );

            _voiceActive = true;
            Debug.Log($"Joined voice channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Voice chat failed: {e.Message}");
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

        // Fire the general session changed event whenever the session changes
        OnSessionChanged?.Invoke(_currentSession);
    }

    private async Task WaitForNetworkShutdownAsync()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        if (networkManager.IsListening)
            networkManager.Shutdown();

        float timeout = 5f;
        float timer = 0f;

        while (networkManager != null && networkManager.IsListening && timer < timeout)
        {
            await Task.Delay(100);
            timer += 0.1f;
        }
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

    #endregion

    #region Hosting

    private async Task<ISession> HostSessionAsync()
    {
        await WaitForNetworkShutdownAsync();

        var session = await CreateNewSessionAsync();

        if (session == null)
            throw new Exception("Session creation failed");

        SetCurrentSession(session);
        OnSessionCreated?.Invoke(session);

        return session;
    }

    private async Task TryHostWithRetryAsync()
    {
        const int maxRetries = 3;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                await WaitForNetworkShutdownAsync();

                Debug.Log($"Host attempt {attempt + 1}");

                var session = await HostSessionAsync();

                if (session != null)
                {
                    Debug.Log("Host migration successful");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Host attempt failed: {e.Message}");
            }

            attempt++;

            await Task.Delay(1500);
        }

        Debug.LogError("All host retry attempts failed.");
    }

    private async Task CleanupNetworkSessionAsync()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await WaitForNetworkShutdownAsync();
        }
    }

    private async Task<ISession> CreateNewSessionAsync()
    {
        var options = new SessionOptions { MaxPlayers = 4 }
            .WithRelayNetwork();

        return await MultiplayerService.Instance.CreateSessionAsync(options);
    }

    #endregion

    #region Joining

    public async Task JoinSessionAsync(string code)
    {
        if (_isBusy || string.IsNullOrWhiteSpace(code))
            return;

        SetBusy(true);

        try
        {
            await WaitForNetworkShutdownAsync();

            var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            SetCurrentSession(session);
            OnSessionJoined?.Invoke(session);
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
            await _currentSession.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed leaving session: {e.Message}");
        }

        SetCurrentSession(null);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await WaitForNetworkShutdownAsync();
        }
    }

    #endregion

    #region Leaving

    public async Task LeaveSessionAsync()
    {
        SetBusy(true);

        await VivoxService.Instance.LeaveAllChannelsAsync();
        _voiceActive = false;

        try
        {
            if (IsHost)
                await EndSessionForAllAsync();
            else
                await LeaveCurrentSessionIfAny();
        }
        finally
        {
            SetCurrentSession(null);

            SceneManager.LoadScene("LobbyScene");
            await WaitForSceneLoadAsync("LobbyScene");

            await TryHostWithRetryAsync();
            SetBusy(false);
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

    #region Host Disconnect

    private void HandleSessionHostChanged(string newHostId)
    {
        if (newHostId != AuthenticationService.Instance.PlayerId)
        {
            OnHostDisconnected?.Invoke();
            _ = HandleHostDisconnectAsync();
        }
    }

    private async Task HandleHostDisconnectAsync()
    {
        SetBusy(true);

        SetCurrentSession(null);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await WaitForNetworkShutdownAsync();
        }

        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            SceneManager.LoadScene("LobbyScene");
            await WaitForSceneLoadAsync("LobbyScene");
        }

        SetBusy(false);
        await TryHostWithRetryAsync();
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

    #region Internal

    private void SetBusy(bool value)
    {
        _isBusy = value;
        OnBusyChanged?.Invoke(value);
    }

    #endregion
}