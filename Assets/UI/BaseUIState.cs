using System.Diagnostics;
using UnityEngine.InputSystem;

public abstract class BaseUIState : PlayerState
{
    protected BaseUIState(PlayerStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        stateMachine.SetPlayerEnabled(false);
    }

    public override void Exit()
    {
        stateMachine.SetPlayerEnabled(true);
        UIManager.Instance.GameUIManager.UnHoverUI();
        stateMachine.Interactor?.ClearHover();
    }

    public override void Tick()
    {
        if (stateMachine.PlayerInputHandler.LastKeyPressed == Key.Escape)
        {
            UnityEngine.Debug.Log("Escape key pressed while in BaseUIState");
            stateMachine.PlayerInputHandler.ResetLastKey(); // making sure the old key is cleared before

            OnEscapePressed();
        }
    }

    protected virtual void OnEscapePressed() => stateMachine.PopState();
}