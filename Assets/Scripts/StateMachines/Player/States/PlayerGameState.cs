public class PlayerGameState : PlayerState
{
    public PlayerGameState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(true);
        stateMachine.LockCursor();

        UIManager.Instance.SessionUIManager.HideLobbyUI();
    }

    public override void Exit()
    {
        // nothing needed
    }
}
