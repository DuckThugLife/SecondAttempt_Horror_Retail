using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpPressed { get; private set; }
    public Key LastKeyPressed { get; private set; }

    private bool movementEnabled = true;


    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
        {
            Move = Vector2.zero;
            JumpPressed = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!movementEnabled) return;
        Move = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!movementEnabled) return;
        JumpPressed = context.performed;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Look = context.ReadValue<Vector2>(); // always active for UI/cursor
    }

    public void OnAnyKey(InputAction.CallbackContext context)
    {
        if (context.performed && context.control is KeyControl keyControl)
            LastKeyPressed = keyControl.keyCode; // always active
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
            LastKeyPressed = Key.Escape; // maps ESC key press to LastKeyPressed
    }

    public void ResetLastKey() => LastKeyPressed = Key.None;
}
