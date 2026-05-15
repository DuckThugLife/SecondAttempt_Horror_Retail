public class PlayerChatState : BaseUIState
{
    public PlayerChatState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        if (MessageController.Instance != null)
        {
            UnityEngine.Debug.Log("ChatState: Entering and calling OpenChatInput");
            MessageController.Instance.OpenChatInput();
        }
        else
        {
            UnityEngine.Debug.LogError("ChatState: MessageController.Instance is MISSING!");
        }
    }

    public override void Exit()
    {
        base.Exit(); // Restores movement (if stack is empty)
        MessageController.Instance.CloseChatInput();
    }
}