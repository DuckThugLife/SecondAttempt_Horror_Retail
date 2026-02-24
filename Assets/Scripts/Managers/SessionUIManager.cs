using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;

public class SessionUIManager : MonoBehaviour
{
    public static SessionUIManager Instance { get; private set; }

    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyRootGO;
    [SerializeField] private TMP_InputField joinInputField;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Button copyButton;

    [Header("Overlays")]
    [SerializeField] private GameObject loadingOverlay;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCreated += HandleSessionChanged;
            SessionManager.Instance.OnSessionJoined += HandleSessionChanged;
            SessionManager.Instance.OnBusyChanged += HandleBusyChanged;
        }
    }

    private void OnDestroy()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCreated -= HandleSessionChanged;
            SessionManager.Instance.OnSessionJoined -= HandleSessionChanged;
            SessionManager.Instance.OnBusyChanged -= HandleBusyChanged;
        }
    }

    private void HandleSessionChanged(ISession session)
    {
        UpdateSessionCode(session);
        ShowLoading(false);
        Debug.Log("Session UI updated");
    }

    private void HandleBusyChanged(bool isBusy)
    {
        ShowLoading(isBusy);
        if (joinInputField != null)
            joinInputField.interactable = !isBusy;
    }

    public void ShowLobbyUI()
    {
        if (lobbyRootGO != null)
            lobbyRootGO.SetActive(true);
    }

    public void HideLobbyUI()
    {
        if (lobbyRootGO != null)
            lobbyRootGO.SetActive(false);
    }

    public void ShowLoading(bool value)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(value);
    }

    public void UpdateSessionCode(ISession session)
    {
        if (session == null) return;

        joinCodeText.text = $"Join Code: {session.Code}";
        copyButton.gameObject.SetActive(true);
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

        ShowLoading(true);
        _ = SessionManager.Instance.JoinSessionAsync(joinCode);
    }
}