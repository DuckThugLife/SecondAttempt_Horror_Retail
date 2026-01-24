using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInputHandler), typeof(Camera))]
public class LocalPlayerRuntimeSetup : NetworkBehaviour
{
    [Header("Local-only components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Ensure non-local players are inert
            if (playerController) playerController.enabled = false;
            if (playerInputHandler) playerInputHandler.enabled = false;
            if (playerCamera) playerCamera.gameObject.SetActive(false);
            if (audioListener) audioListener.enabled = false;
            return;
        }

        // Enable local player systems
        if (playerController) playerController.enabled = true;
        if (playerInputHandler) playerInputHandler.enabled = true;
        if (playerCamera) playerCamera.gameObject.SetActive(true);
        if (audioListener) audioListener.enabled = true;
    }
}
