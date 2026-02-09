using UnityEngine;
public class PlayerLoadingState : PlayerState
{
    public PlayerLoadingState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetGameplayInputEnabled(false);
        stateMachine.UnlockCursor();

        
    }

    public override void Exit()
    {
        
    }

}