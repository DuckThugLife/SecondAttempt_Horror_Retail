public class PlayerChatState : BaseUIState
{
    public PlayerChatState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter(); // Locks movement, unlocks cursor
        MessageController.Instance.OpenChatInput();
    }

    public override void Exit()
    {
        base.Exit(); // Restores movement (if stack is empty)
        MessageController.Instance.CloseChatInput();
    }
}