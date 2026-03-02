using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class MessageManager : NetworkBehaviour
{
    public static MessageManager Instance;
    [SerializeField] private int maxMessageLength = 200; // Same default as the MessageController

    private float lastNameChangeTime;
    private const float NameChangeCooldown = 2f; // Seconds

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    [ClientRpc]
    public void ReplicateMessageClientRPC(string message)
    {
        if (MessageController.Instance != null)
            MessageController.Instance.AddNewMessage(message);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMessageServerRPC(string message, ServerRpcParams serverRpcParams = default)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Server-side validation using same limit as the MessageController
        if (message.Length > maxMessageLength)
            message = message.Substring(0, maxMessageLength);

        ulong clientId = serverRpcParams.Receive.SenderClientId;
        Player player = NetworkObjectManager.Instance.GetPlayer(clientId);
        if (player == null) return;

        if (CheckForCommand(clientId, message))
            return;

        ReplicateMessageClientRPC($"{player.GetUsernameNetworkVar().Value}: {message}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendSystemMessageServerRPC(string message)
    {
        ReplicateMessageClientRPC(message);
    }

    private bool CheckForCommand(ulong clientId, string message)
    {
        if (!message.StartsWith("/"))
            return false;

        string[] parts = message.Split(' ');
        string command = parts[0].ToLower();

        switch (command)
        {
            case "/help":
                ReplicateMessageClientRPC("Available commands: /name, /help");
                return true;

            default:
                ReplicateMessageClientRPC($"Unknown command: {command}");
                return true;
        }
    }

}