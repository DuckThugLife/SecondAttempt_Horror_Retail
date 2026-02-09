using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyRootGO;
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private Button copyButton;

    [Header("Game UI")]
    [SerializeField] private GameObject gameRootGO;

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

        joinCodeField.text = session.Code;
        joinCodeField.interactable = false;
        copyButton.gameObject.SetActive(true);
    }

    public void ClearSessionCode()
    {
        joinCodeField.text = string.Empty;
        joinCodeField.interactable = true;
        copyButton.gameObject.SetActive(false);
    }

    public void CopyJoinCode()
    {
        if (string.IsNullOrEmpty(joinCodeField.text)) return;
        GUIUtility.systemCopyBuffer = joinCodeField.text;
    }
}
