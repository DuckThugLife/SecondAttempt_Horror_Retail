using UnityEngine;
public class PlayerUIState : PlayerState
{
    public PlayerUIState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Machine.PlayerController.enabled = false;
        Machine.UnlockCursor();
    }

    public override void Exit()
    {
        Machine.PlayerController.enabled = true;
    }
}