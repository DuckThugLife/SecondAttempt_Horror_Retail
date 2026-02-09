using UnityEngine;
public class PlayerLoadingState : PlayerState
{
    public PlayerLoadingState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(false); // full lock for loading
        stateMachine.UnlockCursor();
    }

    public override void Exit()
    {
        
    }

}