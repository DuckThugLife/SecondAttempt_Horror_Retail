public class PlayerLobbyState : PlayerState
{
    public PlayerLobbyState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetGameplayInputEnabled(true);
        stateMachine.UnlockCursor();

        UIManager.Instance.ShowLobbyUI();
    }

    public override void Exit()
    {
        // Do nothing
    }
}