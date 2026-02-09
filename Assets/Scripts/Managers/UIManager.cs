using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyRootGO;
    [SerializeField] private TMP_InputField joinInputField;   // Player enters a code here
    [SerializeField] private TMP_Text joinCodeText;           // Shows host session code
    [SerializeField] private Button copyButton;

    [Header("Game UI")]
    [SerializeField] private GameObject gameRootGO;
    [SerializeField] private GameObject hoverIconGO;
    [SerializeField] private GameObject crosshairGO;

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

    // --------------------
    // UI STATE CONTROL
    // --------------------

    public void ShowLobbyUI()
    {
        lobbyRootGO.SetActive(true);
        gameRootGO.SetActive(false);
    }

    public void ShowGameUI()
    {
        lobbyRootGO.SetActive(false);
        gameRootGO.SetActive(true);
    }

    public void ShowLoading(bool value)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(value);
    }

    // --------------------
    // LOBBY-SPECIFIC UI
    // --------------------

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

        // Remove the "Join Code: " prefix before copying
        string code = joinCodeText.text.Replace("Join Code: ", "");
        GUIUtility.systemCopyBuffer = code;
    }

    // --------------------
    // HOVER / CROSSHAIR
    // --------------------

    public void HoverUI()
    {
        hoverIconGO.SetActive(true);
        crosshairGO.SetActive(false);
    }

    public void UnHoverUI()
    {
        hoverIconGO.SetActive(false);
        crosshairGO.SetActive(true);
    }
}
