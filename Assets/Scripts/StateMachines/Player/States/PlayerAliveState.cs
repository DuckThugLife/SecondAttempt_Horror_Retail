public class PlayerAliveState : PlayerState
{
    public PlayerAliveState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(true); // <-- only movement enabled
        stateMachine.LockCursor();
    }

    public override void Exit()
    {
        // nothing needed
    }
}
