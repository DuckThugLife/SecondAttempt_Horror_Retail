using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public static Player Local;

    // Server-only write permission for security
    private NetworkVariable<FixedString64Bytes> usernameNetworkVar
        = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Register with NetworkObjectManager
        if (NetworkObjectManager.Instance != null)
            NetworkObjectManager.Instance.RegisterPlayer(OwnerClientId, this);

        if (IsOwner)
        {
            Local = this;
        }

        // Only the server sets default usernames
        if (IsServer && string.IsNullOrEmpty(usernameNetworkVar.Value.ToString()))
        {
            usernameNetworkVar.Value = $"Player{OwnerClientId}";
        }
    }

    public override void OnNetworkDespawn()
    {
        // Unregister when despawned
        if (NetworkObjectManager.Instance != null)
            NetworkObjectManager.Instance.UnregisterPlayer(OwnerClientId);

        if (IsOwner && Local == this)
            Local = null;
    }

    public NetworkVariable<FixedString64Bytes> GetUsernameNetworkVar()
    {
        return usernameNetworkVar;
    }

    // ServerRPC for clients to request username changes
    [ServerRpc(RequireOwnership = true)]
    private void RequestUsernameChangeServerRPC(FixedString64Bytes requestedUsername)
    {
        // SERVER-SIDE VALIDATION
        string username = requestedUsername.ToString();

        // Basic validation
        if (string.IsNullOrWhiteSpace(username) || username.Length > 20)
        {
            // Invalid - assign default
            usernameNetworkVar.Value = $"Player{OwnerClientId}";
            return;
        }

        // Check for profanity
        if (ContainsProfanity(username))
        {
            usernameNetworkVar.Value = $"Player{OwnerClientId}";
            return;
        }

        // All checks passed - set the username
        usernameNetworkVar.Value = username;
    }

    private bool ContainsProfanity(string username)
    {

        // I Could check against a list, use a service, etc.
        return false; // Placeholder
    }

    // Public method for players to request a username change
    public void RequestUsernameChange(string newUsername)
    {
        if (IsOwner)
        {
            // Always go through server for validation
            RequestUsernameChangeServerRPC(newUsername);
        }
    }
}