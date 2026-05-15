public class PlayerSettingsState : BaseUIState
{
    public PlayerSettingsState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter(); // handles cursor/movement disabling
        SessionUIManager.Instance.UpdateLeaveButtonVisibility();
        UIManager.Instance.SessionUIManager.OpenSettings();
    }

    public override void Exit()
    {
        base.Exit();
        UIManager.Instance.SessionUIManager.CloseSettings();
    }
}