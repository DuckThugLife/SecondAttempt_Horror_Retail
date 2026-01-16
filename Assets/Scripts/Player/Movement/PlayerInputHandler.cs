using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInputHandler : MonoBehaviour
{
    // Expose input values
    public Vector2 Move { get; private set; }
    public bool JumpPressed { get; private set; }
    public Key LastKeyPressed { get; private set; }

    // Called automatically via PlayerInput Actions
    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) JumpPressed = true;
        else if (context.canceled) JumpPressed = false;

    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("LOOKED");
        }
    }

    public void OnAnyKey(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.control is KeyControl keyControl)
                LastKeyPressed = keyControl.keyCode;
        }
    }

}