using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    private CharacterController characterController;
    private PlayerInputHandler inputHandler;

    [Header("Variables")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private float lookSensitivity = 120f;
    [SerializeField] private float lookSmoothTime = 0.05f;

    private Vector2 currentLook;
    private Vector2 lookVelocity;

    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float mouseSensitivity = 120f;
    [SerializeField] private float pitchMin = -60f;
    [SerializeField] private float pitchMax = 75f;

    private float pitch;

    private float verticalVelocity = 0f;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector2 moveInput = inputHandler.Move;
        Vector2 lookInput = inputHandler.Look;
        bool jump = inputHandler.JumpPressed; // only true the frame button is pressed
        

        HandleJump(jump);
        MoveCharacter(moveInput);
        TurnCharacter(lookInput);
    }

    private void HandleJump(bool jump)
    {
        // Apply a small downward force to keep CharacterController grounded detection consistent
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
        // Convert input to world-relative movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= walkSpeed;

        // Add vertical velocity
        move.y = verticalVelocity;

        // Move character
        characterController.Move(move * Time.deltaTime);
    }

    private void TurnCharacter(Vector2 lookInput)
    {
        // Yaw (player)
        float yaw = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * yaw);

        // Pitch (camera pivot)
        pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

}