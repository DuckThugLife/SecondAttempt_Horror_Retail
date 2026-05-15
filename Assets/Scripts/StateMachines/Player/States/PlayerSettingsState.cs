public class PlayerSettingsState : BaseUIState
{
    public PlayerSettingsState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter(); // handles cursor/movement disabling
        UIManager.Instance.SessionUIManager.OpenSettings();
    }

    public override void Exit()
    {
        base.Exit();
        UIManager.Instance.SessionUIManager.CloseSettings();
    }
}