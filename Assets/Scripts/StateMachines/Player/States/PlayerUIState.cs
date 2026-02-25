using UnityEngine.InputSystem;

public class PlayerUIState : PlayerState
{
    public PlayerUIState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(false);
        stateMachine.UnlockCursor();
        stateMachine.PlayerController.DisableTurning();
        UIManager.Instance.SessionUIManager.ShowLobbyUI();
    }

    public override void Exit()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(true);
        stateMachine.LockCursor();
        stateMachine.PlayerController.EnableTurning();
        UIManager.Instance.SessionUIManager.HideLobbyUI();
    }

    public override void Tick()
    {
        // Always get fresh reference from state machine, was getting weird interactions when I joined a player.
        if (stateMachine.PlayerInputHandler == null)
            return;

        if (stateMachine.PlayerInputHandler.LastKeyPressed == Key.Escape)
        {
            // Don't revive dead players!
            if (stateMachine.GetCurrentState() is PlayerDeadState)
            {
                stateMachine.ChangeState(stateMachine.DeadState);
            }
            else
            {
                stateMachine.ChangeState(stateMachine.LobbyState);
            }

            stateMachine.PlayerInputHandler.ResetLastKey();
        }
    }
}