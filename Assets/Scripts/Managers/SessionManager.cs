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


    public async Task JoinSessionAsync(string newCode)
    {
        if (_isBusy || string.IsNullOrWhiteSpace(newCode)) return;

        // Validate join code format
        if (!System.Text.RegularExpressions.Regex.IsMatch(newCode, @"^[A-Z0-9]{6}$"))
        {
            Debug.LogError($"Invalid code format: {newCode}");
            return;
        }

        SetBusy(true);

        string oldCode = _currentSession?.Code;
        ISession oldSession = _currentSession;

        // Leave current session safely
        if (_currentSession != null)
        {
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

        try
        {
            // Attempt to join the new session
            _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(newCode);

            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartClient();

            Debug.Log("Successfully joined new session!");
            // Stay in LobbyScene; UI should update automatically
        }
        catch (Exception joinEx)
        {
            Debug.LogWarning($"Failed to join new session ({newCode}): {joinEx.Message}");

            // Small delay to reduce "Too Many Requests"
            await Task.Delay(1000);

            // Attempt to rejoin the old session
            if (!string.IsNullOrEmpty(oldCode))
            {
                try
                {
                    _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(oldCode);

                    if (!NetworkManager.Singleton.IsListening)
                        NetworkManager.Singleton.StartClient();

                    Debug.Log("Rejoined previous session.");
                    // Stay in LobbyScene
                    return;
                }
                catch (Exception oldJoinEx)
                {
                    Debug.LogWarning($"Failed to rejoin previous session ({oldCode}): {oldJoinEx.Message}");
                }
            }

            // Final fallback: host a new session
            Debug.Log("Hosting new session as fallback.");
            await HostSessionAsync();
            // LobbyScene remains active by default
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
        }   
    }

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
