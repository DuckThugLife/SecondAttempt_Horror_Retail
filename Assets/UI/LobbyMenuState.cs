public class LobbyMenuState : BaseUIState
{
    public LobbyMenuState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        UIManager.Instance.SessionUIManager.ShowLobbyUI();
        base.Enter(); // Movement disabled via BaseUIState
        
    }

    public override void Exit()
    {
        UIManager.Instance.SessionUIManager.HideLobbyUI();
        base.Exit(); // Movement enabled via BaseUIState
    }
}