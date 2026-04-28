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
            OnEscapePressed();
            stateMachine.PlayerInputHandler.ResetLastKey();
        }
    }

    protected abstract void OnEscapePressed();
}