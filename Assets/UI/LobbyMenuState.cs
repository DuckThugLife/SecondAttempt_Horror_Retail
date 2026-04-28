public class LobbyMenuState : BaseUIState
{
    public LobbyMenuState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        base.Enter();
        UIManager.Instance.SessionUIManager.ShowLobbyUI();
    }

    public override void Exit()
    {
        UIManager.Instance.SessionUIManager.HideLobbyUI();
        base.Exit();
    }

    protected override void OnEscapePressed()
    {
        if (stateMachine.GetCurrentState() is PlayerDeadState)
            stateMachine.ChangeState(stateMachine.DeadState);
        else
            stateMachine.ChangeState(stateMachine.LobbyState);
    }
}