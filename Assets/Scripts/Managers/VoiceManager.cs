using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    private bool _voiceActive = false;
    private string _currentVoiceChannel = "";
    private bool _localPlayerReady = false;
    private bool _isJoining = false;
    private bool _eventsSubscribed = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("VoiceManager: Instance created");
    }

    private void OnEnable()
    {
        Debug.Log("VoiceManager OnEnable");

        if (SessionManager.Instance != null)
            SessionManager.Instance.OnSessionChanged += OnSessionChanged;

        TrySubscribeToNetworkEvents();
    }

    private void TrySubscribeToNetworkEvents()
    {
        if (_eventsSubscribed) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            _eventsSubscribed = true;
            Debug.Log("VoiceManager: Subscribed to NetworkManager events");
        }
        else
        {
            Debug.Log("VoiceManager: NetworkManager not ready, will try again later");
            Invoke(nameof(TrySubscribeToNetworkEvents), 0.1f);
        }
    }

    private void OnDisable()
    {
        Debug.Log("VoiceManager OnDisable");

        if (SessionManager.Instance != null)
            SessionManager.Instance.OnSessionChanged -= OnSessionChanged;

        if (NetworkManager.Singleton != null && _eventsSubscribed)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            _eventsSubscribed = false;
        }
    }

    public void OnLocalPlayerReady()
    {
        Debug.Log($"VoiceManager: OnLocalPlayerReady called on {(NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost ? "HOST" : "CLIENT")}");
        _localPlayerReady = true;
        _ = EvaluateVoiceState();
    }

    public void OnLocalPlayerLeft()
    {
        Debug.Log("VoiceManager: OnLocalPlayerLeft called");
        _localPlayerReady = false;
        _ = LeaveVoice();
    }

    private void OnSessionChanged(ISession session)
    {
        Debug.Log($"VoiceManager: OnSessionChanged: {session?.Code}");
        // Force clear voice state when session changes
        _voiceActive = false;
        _currentVoiceChannel = "";
        _ = EvaluateVoiceState();
    }

    private void OnClientConnected(ulong clientId)
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        Debug.Log($"VoiceManager: OnClientConnected {clientId} on {(isHost ? "HOST" : "CLIENT")} - LocalPlayerReady: {_localPlayerReady}, Total Players: {NetworkManager.Singleton?.ConnectedClients?.Count}");

        _ = EvaluateVoiceState();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        Debug.Log($"VoiceManager: OnClientDisconnected {clientId} on {(isHost ? "HOST" : "CLIENT")} - LocalPlayerReady: {_localPlayerReady}, Total Players: {NetworkManager.Singleton?.ConnectedClients?.Count}");

        _ = EvaluateVoiceState();
    }

    private async Task JoinVoice(string channelName)
    {
        if (_isJoining)
        {
            Debug.Log($"VoiceManager: Already joining a voice channel, skipping {channelName}");
            return;
        }

        // Always leave existing channel first - no early returns based on _voiceActive
        if (_voiceActive)
        {
            Debug.Log($"VoiceManager: Leaving current channel {_currentVoiceChannel} before joining {channelName}");
            await LeaveVoice();
        }

        _isJoining = true;
        Debug.Log($"VoiceManager: Joining voice channel: {channelName}");

        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

            _voiceActive = true;
            _currentVoiceChannel = channelName;
            Debug.Log($"VoiceManager: Successfully joined voice: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"VoiceManager: Vivox join failed: {e.Message}");
            _voiceActive = false;
            _currentVoiceChannel = "";
        }
        finally
        {
            _isJoining = false;
        }
    }

    private async Task LeaveVoice()
    {
        if (!_voiceActive && string.IsNullOrEmpty(_currentVoiceChannel))
            return;

        Debug.Log($"VoiceManager: Leaving voice channel {_currentVoiceChannel}");

        try
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();
            Debug.Log("VoiceManager: Left Vivox voice");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"VoiceManager: Vivox leave failed: {e.Message}");
        }
        finally
        {
            _voiceActive = false;
            _currentVoiceChannel = "";
        }
    }

    public async Task ResetVoice()
    {
        Debug.Log("VoiceManager: Resetting voice state");
        await LeaveVoice();
        _localPlayerReady = false;
        _isJoining = false;
    }

    private async Task EvaluateVoiceState()
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        Debug.Log($"=== VoiceManager EvaluateVoiceState on {(isHost ? "HOST" : "CLIENT")} ===");
        Debug.Log($"LocalPlayerReady: {_localPlayerReady}, IsJoining: {_isJoining}");
        Debug.Log($"VoiceActive: {_voiceActive}, CurrentChannel: {_currentVoiceChannel}");

        var session = SessionManager.Instance?.CurrentSession;
        var playerCount = NetworkManager.Singleton?.ConnectedClients?.Count ?? 1;

        Debug.Log($"Session: {session?.Code}, Players: {playerCount}");

        if (session == null)
        {
            Debug.Log("No session, leaving voice");
            if (_voiceActive)
                await LeaveVoice();
            return;
        }

        if (playerCount < 2)
        {
            Debug.Log($"Only {playerCount} players, leaving voice");
            if (_voiceActive)
                await LeaveVoice();
            return;
        }

        // Always join if we're not in the correct channel
        if (!_voiceActive || _currentVoiceChannel != session.Code)
        {
            Debug.Log($"Attempting to join voice channel: {session.Code}");
            await JoinVoice(session.Code);
        }
        else
        {
            Debug.Log("Already in correct voice channel");
        }
    }
}