public class PlayerLobbyState : PlayerState
{
    public PlayerLobbyState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        UIManager.Instance.ShowGameUI();
        stateMachine.PlayerInputHandler.SetMovementEnabled(true); // <-- only movement enabled
        stateMachine.LockCursor();
    }

    public override void Exit()
    {
        // nothing needed
    }
}