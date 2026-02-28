using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public static Player Local;

    private NetworkVariable<FixedString64Bytes> usernameNetworkVar
        = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Register with NetworkObjectManager
        if (NetworkObjectManager.Instance != null)
            NetworkObjectManager.Instance.RegisterPlayer(OwnerClientId, this);

        if (IsOwner)
            Local = this;
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

    [ServerRpc(RequireOwnership = true)]
    private void SetUsernameServerRPC(FixedString64Bytes newUsername)
    {
        SetUsername(newUsername.ToString());
    }

    public void SetUsername(string newUsername)
    {
        usernameNetworkVar.Value = newUsername;
    }
}