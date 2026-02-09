using System;
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

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        NetBootstrap.OnServicesInitialized += HandleServicesReady;
    }

    private void OnDisable()
    {
        NetBootstrap.OnServicesInitialized -= HandleServicesReady;
    }

    // --------------------
    // SOLO
    // --------------------

    public void StartSoloGame()
    {
        if (_isBusy || NetworkManager.Singleton.IsListening) return;
        NetworkManager.Singleton.StartHost();
        StartGameForAllPlayers();
    }

    // --------------------
    // HOST
    // --------------------

    public async Task HostSessionAsync(int maxPlayers = 4)
    {
        if (_isBusy || !NetBootstrap.IsInitialized) return;

        SetBusy(true);

        try
        {
            if (NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            _currentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);

            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartHost();

            // Update the join code field for the host
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

    // --------------------
    // JOIN
    // --------------------

    public async Task JoinSessionAsync(string code)
    {
        if (_isBusy || string.IsNullOrWhiteSpace(code)) return;
        if (NetworkManager.Singleton.IsListening) return;

        SetBusy(true);

        try
        {
            _currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"Join failed: {e.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // --------------------
    // GAME FLOW
    // --------------------

    public void StartGameForAllPlayers()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single
        );
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
        }    }

    // --------------------
    // INTERNAL
    // --------------------

    private void SetBusy(bool value)
    {
        _isBusy = value;
        OnBusyChanged?.Invoke(value);
    }

    private async void HandleServicesReady()
    {
        SceneManager.LoadScene("LobbyScene");
        await HostSessionAsync();
    }
}
