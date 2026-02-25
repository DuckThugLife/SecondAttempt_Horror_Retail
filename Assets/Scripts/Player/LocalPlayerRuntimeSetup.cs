using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerInputHandler), typeof(PlayerInput))]
public class LocalPlayerRuntimeSetup : NetworkBehaviour
{
    [Header("Local-only components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private PlayerInput playerInput; 
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // For non-local players, disable ALL input components
            if (playerController) playerController.enabled = false;

            // Disable the Unity PlayerInput component
            if (playerInput) playerInput.enabled = false;

            // Disable your custom handler too
            if (playerInputHandler) playerInputHandler.enabled = false;

            if (playerCamera) playerCamera.gameObject.SetActive(false);
            if (audioListener) audioListener.enabled = false;
            return;
        }

        // Enable local player systems
        if (playerController) playerController.enabled = true;
        if (playerInput) playerInput.enabled = true;
        if (playerInputHandler)
        {
            playerInputHandler.enabled = true;
            playerInputHandler.SetMovementEnabled(true);
        }
        if (playerCamera) playerCamera.gameObject.SetActive(true);
        if (audioListener) audioListener.enabled = true;
    }
}