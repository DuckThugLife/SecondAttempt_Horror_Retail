public class ChatState : BaseUIState
{
    public ChatState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        base.Enter(); // Disables player
        MessageController.Instance?.OpenChat();
    }

    public override void Exit()
    {
        base.Exit(); // Re-enables player
        MessageController.Instance?.CloseChat();
    }

    protected override void OnEscapePressed()
    {
        stateMachine.PopState();
    }
}