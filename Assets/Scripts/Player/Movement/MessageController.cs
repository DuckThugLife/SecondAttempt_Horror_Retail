using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MessageController : MonoBehaviour
{
    public static MessageController Instance { get; private set; }
    public static bool IsChatFocused => Instance != null && Instance.chatInput != null && Instance.chatInput.isFocused;

    [Header("Chat UI")]
    [SerializeField] private GameObject chatLogPanel;
    [SerializeField] private GameObject chatInputPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private TMP_Text chatMessagePrefab;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private int maxMessageHistory = 10;

    [Header("Chat Settings")]
    [SerializeField] private bool chatVisibleByDefault = false; // False for horror mode
    [SerializeField] private float visibleDuration = 5f;
    [SerializeField] private CanvasGroup chatLogCanvasGroup;
    [SerializeField] private float fadedAlpha = 0f; // Fully transparent

    private Queue<TMP_Text> messageHistory;
    private PlayerStateMachine localPlayer => PlayerStateMachine.LocalInstance;
    private bool _isInputOpen = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        messageHistory = new Queue<TMP_Text>();
        chatLogPanel.SetActive(true);

        // Always start invisible
        if (chatLogCanvasGroup != null)
            chatLogCanvasGroup.alpha = 0f;

        chatInputPanel.SetActive(false);
        chatInput.onValueChanged.AddListener(OnChatInputChanged);
        chatInput.onDeselect.AddListener(delegate { Clear(); });
    }

    private void Update()
    {
        if (localPlayer == null) return;

        // Open chat input with Enter
        if (localPlayer.PlayerInputHandler.LastKeyPressed == Key.Enter)
        {
            if (!_isInputOpen)
            {
                OpenChatInput();
            }
            localPlayer.PlayerInputHandler.ResetLastKey();
        }

        // Close chat input with Escape
        if (_isInputOpen && localPlayer.PlayerInputHandler.LastKeyPressed == Key.Escape)
        {
            CloseChatInput();
            localPlayer.PlayerInputHandler.ResetLastKey();
        }
    }

    private void OpenChatInput()
    {
        // Don't open chat if any UI menu is open
        if (PlayerStateMachine.LocalInstance?.CurrentState is BaseUIState)
            return;

        _isInputOpen = true;
        chatInputPanel.SetActive(true);
        chatInput.interactable = true;
        chatInput.ActivateInputField();

        // Disable player movement while typing
        localPlayer?.SetPlayerEnabled(false);

        // Make chat log fully visible when typing
        if (chatLogCanvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            chatLogCanvasGroup.alpha = 1f;
        }
    }

    private void CloseChatInput()
    {
        _isInputOpen = false;
        chatInputPanel.SetActive(false);
        chatInput.interactable = false;

        // Re-enable player movement
        localPlayer?.SetPlayerEnabled(true);

        // Start fade after typing
        if (chatLogCanvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeChatLog());
        }
    }

    private IEnumerator FadeChatLog()
    {
        yield return new WaitForSeconds(visibleDuration);

        float elapsed = 0f;
        float duration = 1f;

        while (elapsed < duration)
        {
            chatLogCanvasGroup.alpha = Mathf.Lerp(1f, fadedAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        chatLogCanvasGroup.alpha = fadedAlpha;
    }

    private async void Clear()
    {
        await Task.Yield();
        chatInput.text = "";
        chatInput.interactable = false;
    }

    private void OnChatInputChanged(string newValue)
    {
        if (!newValue.EndsWith("\n")) return;
        string newMessage = newValue.Remove(newValue.Length - 1);

        if (!string.IsNullOrWhiteSpace(newMessage))
            MessageManager.Instance.SendMessageServerRPC(newMessage);

        Clear();
        CloseChatInput();
    }

    public void AddNewMessage(string message)
    {
        // Make chat log visible when new message arrives
        if (chatLogCanvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            chatLogCanvasGroup.alpha = 1f;
            fadeCoroutine = StartCoroutine(FadeChatLog());
        }

        if (messageHistory.Count >= maxMessageHistory)
        {
            Destroy(messageHistory.Dequeue().gameObject);
        }

        TMP_Text newMessage = Instantiate(chatMessagePrefab, scrollContent);
        newMessage.text = message;
        messageHistory.Enqueue(newMessage);

        // Auto-scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}