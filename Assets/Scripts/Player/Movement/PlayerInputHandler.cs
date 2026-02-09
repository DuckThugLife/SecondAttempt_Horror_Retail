using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpPressed { get; private set; }
    public Key LastKeyPressed { get; private set; }

    private bool gameplayInputEnabled = true;

    public void SetGameplayInputEnabled(bool enabled)
    {
        gameplayInputEnabled = enabled;

        if (!enabled)
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            JumpPressed = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!gameplayInputEnabled) return;
        Move = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!gameplayInputEnabled) return;

        if (context.performed) JumpPressed = true;
        else if (context.canceled) JumpPressed = false;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!gameplayInputEnabled) return;
        Look = context.ReadValue<Vector2>();
    }

    public void OnAnyKey(InputAction.CallbackContext context)
    {
        if (!gameplayInputEnabled) return;

        if (context.performed && context.control is KeyControl keyControl)
            LastKeyPressed = keyControl.keyCode;
    }
}
