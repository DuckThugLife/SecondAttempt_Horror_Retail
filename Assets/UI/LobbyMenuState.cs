public class LobbyMenuState : BaseUIState
{
    public LobbyMenuState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        base.Enter(); // Movement disabled via BaseUIState
        UIManager.Instance.SessionUIManager.ShowLobbyUI();
    }

    public override void Exit()
    {
        UIManager.Instance.SessionUIManager.HideLobbyUI();
        base.Exit(); // Movement enabled via BaseUIState
    }
}