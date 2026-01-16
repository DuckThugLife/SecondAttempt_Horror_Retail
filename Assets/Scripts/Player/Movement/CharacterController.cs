using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    private CharacterController characterController;
    private PlayerInputHandler inputHandler;

    [Header("Variables")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float gravity = -9.81f;

    private float verticalVelocity = 0f;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector2 moveInput = inputHandler.Move;
        bool jump = inputHandler.JumpPressed; // only true the frame button is pressed

        HandleJump(jump);
        MoveCharacter(moveInput);
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
}