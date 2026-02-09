public class PlayerLobbyState : PlayerState
{
    public PlayerLobbyState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        UIManager.Instance.ShowGameUI();
        stateMachine.PlayerInputHandler.SetGameplayInputEnabled(true);
        stateMachine.LockCursor();
       
    }

    public override void Exit()
    {
        // Do nothing
    }
}