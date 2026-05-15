using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.SceneManagement;

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

    [SerializeField] private Button startGameButton;

    [Header("Settings Menu")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;

    [Header("Name Change")]
    [SerializeField] private TMP_InputField nameChangeInput;
    [SerializeField] private Button saveNameChangeButton;

    [Header("Overlays")]
    [SerializeField] private GameObject loadingOverlay;

    public static SessionUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Wire up buttons if not done in inspector
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseSettingsClicked);

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

    private void OnDestroy()
    {

        if (saveNameChangeButton != null)
            saveNameChangeButton.onClick.RemoveListener(OnChangeNameClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseSettingsClicked);

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionChanged -= HandleSessionChanged;
            SessionManager.Instance.OnBusyChanged -= HandleBusyChanged;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientCountChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientCountChanged;
        }
    }

    private void SubscribeToEvents()
    {
        SessionManager.Instance.OnSessionChanged += HandleSessionChanged;
        SessionManager.Instance.OnBusyChanged += HandleBusyChanged;
    }
   


    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OnCloseSettingsClicked()
    {
        // Check if we are actually in the Settings State before popping
        if (PlayerStateMachine.LocalInstance.CurrentState is PlayerSettingsState)
        {
            PlayerStateMachine.LocalInstance.PopState();
        }
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



    private void OnClientCountChanged(ulong clientId)
    {
        UpdateLeaveButtonVisibility();
        UpdateStartButtonVisibility();
    }

    public void UpdateLeaveButtonVisibility()
    {
        if (leaveButton == null) return;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        // 1. If you're a client, you're never trapped.
        if (!isHost)
        {
            leaveButton.gameObject.SetActive(true);
            return;
        }

        // 2. If you're the Host, check the scene and player count
        bool isGameScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene";

        if (isGameScene)
        {
            leaveButton.gameObject.SetActive(true); // Host can always end the game
        }
        else
        {
            leaveButton.gameObject.SetActive(GetPlayerCount() > 1); // Lobby restriction
        }
    }

    private void UpdateStartButtonVisibility()
    {
        if (leaveButton == null) return;

        bool isHost = SessionManager.Instance != null && SessionManager.Instance.IsHost;
        bool isInGame = PlayerStateMachine.LocalInstance.GetCurrentState() is PlayerGameState;
        if (isHost)
        {
            // If we are in the actual game, Host MUST see the leave button to quit, same thing for other players, they gotta be able to leave.
            if (isInGame)
            {
                leaveButton.gameObject.SetActive(true);
            }
            else // If we are in the lobby, use the "more than 1 player" rule, no point in leaving a lobby when you are by yourself.
            {
                leaveButton.gameObject.SetActive(GetPlayerCount() > 1);
            }
        }
        else
        {
            // Clients can always leave
            leaveButton.gameObject.SetActive(true);
        }
    }

    private int GetPlayerCount()
    {
        return NetworkManager.Singleton?.ConnectedClients?.Count ?? 1;
    }

    private void HandleSessionChanged(ISession session)
    {
        if (session == null)
        {
            ClearSessionCode();
            ShowLoading(false);
            UpdateLeaveButtonVisibility();
            UpdateStartButtonVisibility();
            return;
        }

        Debug.Log($"HandleSessionChanged: {session.Code}");

        UpdateSessionCode(session);
        ShowLoading(false);
        UpdateLeaveButtonVisibility();
        UpdateStartButtonVisibility();

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