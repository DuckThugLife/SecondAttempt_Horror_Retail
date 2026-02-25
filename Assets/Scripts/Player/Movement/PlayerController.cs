using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : NetworkBehaviour
{
    
    [Header("Class References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerInputHandler inputHandler;


    [Header("Variables")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private Transform cameraPivot;

    [SerializeField] private float mouseSensitivity = 120f;
    [SerializeField] private float pitchMin = -60f;
    [SerializeField] private float pitchMax = 75f;

    [field: SerializeField] private bool turningEnabled = true;

    private float pitch;
    private float verticalVelocity = 0f;



    void Update()
    {
        if (!IsOwner) return; // Ownership gating

        Vector2 moveInput = inputHandler.Move;
        Vector2 lookInput = inputHandler.Look;
        bool jump = inputHandler.JumpPressed;

        HandleJump(jump);
        MoveCharacter(moveInput);
        TurnCharacter(lookInput);
    }

    private void HandleJump(bool jump)
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -0.1f;

            if (jump)
                verticalVelocity = jumpPower;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void MoveCharacter(Vector2 moveInput)
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= walkSpeed;
        move.y = verticalVelocity;

        characterController.Move(move * Time.deltaTime);
    }

    private void TurnCharacter(Vector2 lookInput)
    {
        if (!turningEnabled) return;

        float yaw = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * yaw);

        pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void EnableTurning()
    {
        turningEnabled = true;
    }
    public void DisableTurning()
    {
        turningEnabled = false;
    }
}
