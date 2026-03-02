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
            case "/name":
                if (parts.Length > 1)
                {
                    // Check cooldown
                    if (Time.time - lastNameChangeTime < NameChangeCooldown)
                    {
                        float remaining = NameChangeCooldown - (Time.time - lastNameChangeTime);
                        ReplicateMessageClientRPC($"Please wait {remaining:F1}s before changing name again");
                        return true;
                    }

                    string newName = string.Join(" ", parts, 1, parts.Length - 1);
                    if (newName.Length > 20) newName = newName.Substring(0, 20);

                    Player player = NetworkObjectManager.Instance?.GetPlayer(clientId);
                    if (player != null)
                    {
                        lastNameChangeTime = Time.time;
                        string oldName = player.GetUsernameNetworkVar().Value.ToString();

                        player.RequestUsernameChange(newName);

                        if (player.IsOwner)
                        {
                            PlayerPrefs.SetString("PlayerName", newName);
                            PlayerPrefs.Save();
                        }

                        ReplicateMessageClientRPC($"<color=yellow>{oldName} changed name to {newName}</color>");
                    }
                }
                else
                {
                    ReplicateMessageClientRPC("Usage: /name <newname>");
                }
                return true;

            case "/help":
                ReplicateMessageClientRPC("Available commands: /name, /help");
                return true;

            default:
                ReplicateMessageClientRPC($"Unknown command: {command}");
                return true;
        }
    }

}