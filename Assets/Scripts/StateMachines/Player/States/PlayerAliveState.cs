using UnityEngine;
public class PlayerAliveState : PlayerState
{
    public PlayerAliveState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetGameplayInputEnabled(true);
        stateMachine.LockCursor();
    }

    public override void Exit()
    {
       
    }
}