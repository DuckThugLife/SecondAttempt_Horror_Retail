public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(false); // block movement only
        stateMachine.UnlockCursor();
    }

    public override void Exit() { }
}
