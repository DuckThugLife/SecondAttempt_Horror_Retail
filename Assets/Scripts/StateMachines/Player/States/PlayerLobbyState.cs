using UnityEngine;
public class PlayerLobbyState : PlayerState
{
    public PlayerLobbyState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Machine.PlayerController.enabled = true;
        Machine.LockCursor();
    }

    public override void Exit()
    {
        Machine.PlayerController.enabled = false;
        Machine.PlayerInputHandler.enabled = false;
    }
}