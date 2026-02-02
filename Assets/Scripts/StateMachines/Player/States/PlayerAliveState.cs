using UnityEngine;
public class PlayerAliveState : PlayerState
{
    public PlayerAliveState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Machine.PlayerController.enabled = true;
        Machine.LockCursor();
    }

    public override void Exit()
    {
        Machine.PlayerController.enabled = false;
    }
}