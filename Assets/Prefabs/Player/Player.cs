using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public static Player Local;
    public event Action<string> OnUsernameChanged;

    private NetworkVariable<FixedString64Bytes> usernameNetworkVar
        = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        usernameNetworkVar.OnValueChanged += (oldValue, newValue) =>
        {
            OnUsernameChanged?.Invoke(newValue.ToString());
            Debug.Log($"Username changed from {oldValue} to {newValue}");
        };
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Player {OwnerClientId}] OnNetworkSpawn - IsServer: {IsServer}, IsOwner: {IsOwner}");

        if (NetworkObjectManager.Instance != null)
            NetworkObjectManager.Instance.RegisterPlayer(OwnerClientId, this);

        if (IsServer)
        {
            StartCoroutine(SendJoinMessageWhenReady());
        }

        if (IsOwner)
        {
            Local = this;
            string savedName = PlayerPrefs.GetString("PlayerName", $"Player{OwnerClientId}");
            Debug.Log($"[Player {OwnerClientId}] Requesting username: {savedName}");
            RequestUsernameChangeServerRPC(savedName);
        }
    }

    private IEnumerator SendJoinMessageWhenReady()
    {
        Debug.Log($"[Player {OwnerClientId}] Waiting for MessageManager...");

        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool messageManagerReady = MessageManager.Instance != null &&
                                       MessageManager.Instance.IsSpawned;
            bool usernameReady = !string.IsNullOrEmpty(usernameNetworkVar.Value.ToString());

            if (messageManagerReady && usernameReady)
            {
                string name = GetUsername();
                Debug.Log($"[Player {OwnerClientId}] Sending join message for {name}");
                MessageManager.Instance.SendSystemMessageServerRPC($"<color=yellow>{name} joined the lobby</color>");
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        Debug.LogError($"[Player {OwnerClientId}] Timeout waiting for conditions!");
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && MessageManager.Instance != null && MessageManager.Instance.IsSpawned)
        {
            string name = GetUsername();
            MessageManager.Instance.SendSystemMessageServerRPC($"<color=red>{name} left the lobby</color>");
        }

        if (NetworkObjectManager.Instance != null)
            NetworkObjectManager.Instance.UnregisterPlayer(OwnerClientId);

        if (IsOwner && Local == this)
            Local = null;
    }

    private string GetUsername()
    {
        string name = usernameNetworkVar.Value.ToString();
        return string.IsNullOrEmpty(name) ? $"Player{OwnerClientId}" : name;
    }

    public NetworkVariable<FixedString64Bytes> GetUsernameNetworkVar()
    {
        return usernameNetworkVar;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestUsernameChangeServerRPC(FixedString64Bytes requestedUsername)
    {
        string username = requestedUsername.ToString();

        if (string.IsNullOrWhiteSpace(username) || username.Length > 20)
        {
            usernameNetworkVar.Value = $"Player{OwnerClientId}";
            return;
        }

        usernameNetworkVar.Value = username;
        Debug.Log($"[Server] Set username for {OwnerClientId} to {username}");
    }

    // Public method for owners to request changes
    public void RequestUsernameChange(string newUsername)
    {
        if (IsOwner)
        {
            RequestUsernameChangeServerRPC(newUsername);
        }
    }
}