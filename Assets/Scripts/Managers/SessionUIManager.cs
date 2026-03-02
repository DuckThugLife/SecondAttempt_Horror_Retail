using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Netcode;

public class SessionUIManager : MonoBehaviour
{
    public static bool IsAnyInputFocused =>
        UIManager.Instance != null &&
        UIManager.Instance.SessionUIManager != null &&
        UIManager.Instance.SessionUIManager.joinInputField != null &&
        UIManager.Instance.SessionUIManager.joinInputField.isFocused;

    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyRootGO;
    [SerializeField] private TMP_InputField joinInputField;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Button copyButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button joinButton;

    [Header("Settings Menu")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;

    [Header("Name Change")]
    [SerializeField] private TMP_InputField nameChangeInput;
    [SerializeField] private Button saveNameChangeButton;

    [Header("Overlays")]
    [SerializeField] private GameObject loadingOverlay;


    private void Start()
    {
        // Wire up buttons if not done in inspector
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);

        if (saveNameChangeButton != null)
            saveNameChangeButton.onClick.AddListener(OnChangeNameClicked);

        // Start with settings closed
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private async void OnEnable()
    {
        Debug.Log($"SessionUIManager OnEnable - SessionManager.Instance exists: {SessionManager.Instance != null}");

        if (SessionManager.Instance != null)
        {
            SubscribeToEvents();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientCountChanged;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientCountChanged;
            }
        }
        else
        {
            Debug.Log("SessionManager not ready yet - waiting...");
            await WaitForSessionManager();
        }
    }


    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        UIManager.Instance.SetUIEnabled(true); 
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        UIManager.Instance.SetUIEnabled(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel.activeSelf)
            CloseSettings();
        else
            OpenSettings();
    }

    public bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    private async Task WaitForSessionManager()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (SessionManager.Instance != null)
            {
                Debug.Log("SessionManager now available, subscribing to events");
                SubscribeToEvents();

                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientCountChanged;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientCountChanged;
                }
                return;
            }

            await Task.Delay(100);
            elapsed += 0.1f;
        }

        Debug.LogError("Failed to find SessionManager after timeout!");
    }

    private void SubscribeToEvents()
    {
        SessionManager.Instance.OnSessionCreated += HandleSessionChanged;
        SessionManager.Instance.OnSessionJoined += HandleSessionChanged;
        SessionManager.Instance.OnBusyChanged += HandleBusyChanged;
    }

    private void OnDestroy()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCreated -= HandleSessionChanged;
            SessionManager.Instance.OnSessionJoined -= HandleSessionChanged;
            SessionManager.Instance.OnBusyChanged -= HandleBusyChanged;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientCountChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientCountChanged;
        }
    }

    private void OnClientCountChanged(ulong clientId)
    {
        UpdateLeaveButtonVisibility();
    }

    private void UpdateLeaveButtonVisibility()
    {
        if (leaveButton == null) return;

        bool isHost = SessionManager.Instance != null && SessionManager.Instance.IsHost;
        int playerCount = NetworkManager.Singleton?.ConnectedClients?.Count ?? 1;

        if (isHost)
        {
            leaveButton.gameObject.SetActive(playerCount > 1);
        }
        else
        {
            leaveButton.gameObject.SetActive(true);
        }
    }

    private void HandleSessionChanged(ISession session)
    {
        Debug.Log($"HandleSessionChanged called with code: {session?.Code}");
        UpdateSessionCode(session);
        ShowLoading(false);
        UpdateLeaveButtonVisibility();

        bool isHost = SessionManager.Instance != null && SessionManager.Instance.IsHost;
        if (joinInputField != null)
            joinInputField.gameObject.SetActive(isHost);
        if (joinButton != null)
            joinButton.gameObject.SetActive(isHost);
    }

    private void HandleBusyChanged(bool isBusy)
    {
        ShowLoading(isBusy);
        if (joinInputField != null)
            joinInputField.interactable = !isBusy;
    }

    public void ShowLobbyUI() => lobbyRootGO?.SetActive(true);
    public void HideLobbyUI() => lobbyRootGO?.SetActive(false);
    public void ShowLoading(bool value) => loadingOverlay?.SetActive(value);

    public void UpdateSessionCode(ISession session)
    {
        if (session == null)
        {
            Debug.LogError("UpdateSessionCode: session is null!");
            return;
        }

        Debug.Log($"UpdateSessionCode: joinCodeText is null? {joinCodeText == null}");
        if (joinCodeText != null)
        {
            joinCodeText.text = $"Join Code: {session.Code}";
            copyButton.gameObject.SetActive(true);
            Debug.Log($"Text set to: {joinCodeText.text}");
        }
    }

    public void ClearSessionCode()
    {
        joinCodeText.text = "Join Code:";
        copyButton.gameObject.SetActive(false);
    }

    public void CopyJoinCode()
    {
        if (string.IsNullOrEmpty(joinCodeText.text)) return;
        string code = joinCodeText.text.Replace("Join Code: ", "");
        GUIUtility.systemCopyBuffer = code;
    }

    public void JoinSessionWithCode()
    {
        if (SessionManager.Instance == null) return;
        string joinCode = joinInputField.text;
        if (string.IsNullOrEmpty(joinCode)) return;

        if (PlayerStateMachine.LocalInstance != null &&
            PlayerStateMachine.LocalInstance.GetCurrentState() is BaseUIState)
        {
            PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LoadingState);
        }

        ShowLoading(true);
        _ = SessionManager.Instance.JoinSessionAsync(joinCode);
    }

    public void OnLeaveButtonClicked()
    {
        if (PlayerStateMachine.LocalInstance != null &&
            PlayerStateMachine.LocalInstance.GetCurrentState() is BaseUIState)
        {
            PlayerStateMachine.LocalInstance.ChangeState(PlayerStateMachine.LocalInstance.LobbyState);
        }

        if (SessionManager.Instance != null)
            _ = SessionManager.Instance.LeaveSessionAsync();
    }

    public void OnChangeNameClicked()
    {
        if (Player.Local == null) return;

        string newName = nameChangeInput.text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            string oldName = Player.Local.GetUsernameNetworkVar().Value.ToString();

            // Request name change
            Player.Local.RequestUsernameChange(newName);

            // Save to PlayerPrefs
            PlayerPrefs.SetString("PlayerName", newName);
            PlayerPrefs.Save();

            // Broadcast name change via message system (same as /name command)
            MessageManager.Instance?.SendSystemMessageServerRPC(
                $"<color=yellow>{oldName} changed name to {newName}</color>"
            );

            nameChangeInput.text = "";
            Debug.Log($"Name changed to: {newName}");
        }
    }
}