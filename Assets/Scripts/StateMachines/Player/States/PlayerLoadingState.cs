using UnityEngine;
public class PlayerLoadingState : PlayerState
{
    public PlayerLoadingState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(false);
        stateMachine.UnlockCursor();

        // Show loading, hide lobby
        UIManager.Instance.SessionUIManager.ShowLoading(true);
        UIManager.Instance.SessionUIManager.HideLobbyUI();
    }

    public override void Exit()
    {
        
    }

}