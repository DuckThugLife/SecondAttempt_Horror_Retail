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

    [SerializeField] private GameObject chatParent;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private TMP_Text chatMessagePrefab;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private int maxMessageHistory = 10;

    private Queue<TMP_Text> messageHistory;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        messageHistory = new Queue<TMP_Text>();
        chatParent.SetActive(false);

        chatInput.onValueChanged.AddListener(OnChatInputChanged);
        chatInput.onDeselect.AddListener(delegate { Clear(); });
    }

    // Called by ChatState.Enter()
    public void OpenChat()
    {
        chatParent.SetActive(true);
        FocusChat();
    }

    // Called by ChatState.Exit()
    public void CloseChat()
    {
        chatParent.SetActive(false);
        chatInput.interactable = false;
    }

    private void FocusChat()
    {
        chatInput.interactable = true;
        chatInput.ActivateInputField();
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
        CloseChat();

        // Close chat state after sending
        PlayerStateMachine.LocalInstance?.PopState();
    }

    public void AddNewMessage(string message)
    {
        if (messageHistory.Count >= maxMessageHistory)
        {
            Destroy(messageHistory.Dequeue().gameObject);
        }

        TMP_Text newMessage = Instantiate(chatMessagePrefab, scrollContent);
        newMessage.text = message;
        messageHistory.Enqueue(newMessage);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}