using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField joinCodeField; // Input AND Display
    [SerializeField] private Button copyButton;           // Hidden until code exists
    [SerializeField] private GameObject loadingOverlay;

    private ISession _currentSession;
    private bool _isBusy;

    // --- SOLO GAME LOGIC ---
    public void StartSoloGame()
    {
        if (_isBusy || NetworkManager.Singleton.IsListening) return;

        // Solo doesn't need Unity Services; it just starts the local engine
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Solo game started.");
            StartGameForAllPlayers();
        }
    }

    // --- MULTIPLAYER HOST LOGIC ---
    public async void HostSession(int maxPlayers = 4)
    {
        if (_isBusy || !NetBootstrap.IsInitialized) return;

        // Ensure NetworkManager is totally clean before starting
        if (NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();

        SetLoadingState(true);
        try
        {
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();

            // This call often triggers NetworkManager.StartHost() automatically
            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            // FIX: Only call StartHost manually if the SDK didn't already start it
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }

            // Update UI with the new code
            if (_currentSession != null)
            {
                joinCodeField.text = _currentSession.Code;
                joinCodeField.interactable = false;
                if (copyButton != null) copyButton.gameObject.SetActive(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Host Session Failed: {e.Message}");
            _currentSession = null;
        }
        finally { SetLoadingState(false); }
    }

    // --- MULTIPLAYER JOIN LOGIC ---
    public async void JoinSessionFromUI()
    {
        if (joinCodeField == null) return;
        await JoinSession(joinCodeField.text.Trim());
    }

    public async Task JoinSession(string code)
    {
        if (_isBusy || string.IsNullOrEmpty(code) || NetworkManager.Singleton.IsListening) return;

        SetLoadingState(true);
        try
        {
            _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            // Similar to Host, join might auto-start. Check first.
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartClient();
            }

            joinCodeField.interactable = false;
        }
        catch (Exception e) { Debug.LogError($"Join failed: {e.Message}"); }
        finally { SetLoadingState(false); }
    }

    // --- UTILITIES ---
    public void StartGameForAllPlayers()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        // Use NetworkSceneManager to sync all clients
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void CopyCodeToClipboard()
    {
        if (joinCodeField == null || string.IsNullOrEmpty(joinCodeField.text)) return;
        GUIUtility.systemCopyBuffer = joinCodeField.text;
    }

    private void SetLoadingState(bool busy)
    {
        _isBusy = busy;
        if (loadingOverlay != null) loadingOverlay.SetActive(busy);
    }

    public async void LeaveSession()
    {
        if (_currentSession != null) await _currentSession.LeaveAsync();
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();

        // Reset UI for next time
        joinCodeField.text = "";
        joinCodeField.interactable = true;
        if (copyButton != null) copyButton.gameObject.SetActive(false);

        SceneManager.LoadScene("LobbyScene");
    }
}