using UnityEngine;
public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
      stateMachine.PlayerInputHandler.SetGameplayInputEnabled(false);
        //Machine.LockCursor();
    }

    public override void Exit()
    {
        // Nothing for now
    }
}