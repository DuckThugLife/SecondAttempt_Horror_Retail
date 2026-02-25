using UnityEngine.InputSystem;

public class PlayerUIState : PlayerState
{
    public PlayerUIState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(false); // only movement blocked
        stateMachine.UnlockCursor(); // unlocks AND makes cursor visible
        stateMachine.PlayerController.DisableTurning();
    }

    public override void Exit()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(true);
        stateMachine.LockCursor();   // locks AND hides cursor
        UIManager.Instance.SessionUIManager.HideLobbyUI();
        stateMachine.PlayerController.EnableTurning();
    }

    public override void Tick()
    {
        if (stateMachine.PlayerInputHandler.LastKeyPressed == Key.Escape)
        {
            stateMachine.RevertToPreviousState();  // go back to whatever state was active
            stateMachine.PlayerInputHandler.ResetLastKey();
        }
    }
}
