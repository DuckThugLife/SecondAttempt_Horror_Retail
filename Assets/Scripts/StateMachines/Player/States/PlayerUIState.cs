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

        if (UIManager.Instance != null && UIManager.Instance.SessionUIManager != null)
            UIManager.Instance.SessionUIManager.HideLobbyUI();

        // Reset hover state
        if (UIManager.Instance != null && UIManager.Instance.GameUIManager != null)
            UIManager.Instance.GameUIManager.UnHoverUI();

        // Clear any lingering hover
        if (stateMachine.Interactor != null)
            stateMachine.Interactor.ClearHover();
    }

    public override void Tick()
    {
        if (stateMachine.PlayerInputHandler == null)
            return;

        if (stateMachine.PlayerInputHandler.LastKeyPressed == Key.Escape)
        {
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