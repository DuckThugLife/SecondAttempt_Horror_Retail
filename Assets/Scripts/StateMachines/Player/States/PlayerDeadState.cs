using UnityEngine;
public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Machine.PlayerController.enabled = false;
        //Machine.LockCursor();
    }

    public override void Exit()
    {
        Machine.PlayerController.enabled = false;
    }
}