using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class EnableOwnerCamera : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        var input = GetComponent<PlayerInput>();
        input.camera.gameObject.SetActive(true);
        Debug.Log($"Camera's enabled for {gameObject.name}");
    }
}

