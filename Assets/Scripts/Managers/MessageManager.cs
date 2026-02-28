using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class MessageManager : NetworkBehaviour
{
    public static MessageManager Instance;

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

        ulong clientId = serverRpcParams.Receive.SenderClientId;
        Player player = NetworkObjectManager.Instance.GetPlayer(clientId);
        if (player == null) return;

        if (CheckForCommand(clientId, message))
            return;

        ReplicateMessageClientRPC($"{player.GetUsernameNetworkVar().Value}: {message}");
    }

    private bool CheckForCommand(ulong clientId, string message)
    {
        if (!message.StartsWith("/"))
            return false;

        return false;
    }
}