using UnityEngine;

public class JoinBoard : MonoBehaviour, IInteractable, IHoverable
{
    private bool _isHovered;

    public void HoverEnter(Interactor interactor)
    {
        if (_isHovered) return;
        _isHovered = true;
        UIManager.Instance.GameUIManager.HoverUI();
    }

    public void HoverExit(Interactor interactor)
    {
        if (!_isHovered) return;
        _isHovered = false;
        UIManager.Instance.GameUIManager.UnHoverUI();
    }

    public void Interact(Interactor interactor)
    {
        if (!_isHovered) return;

        // Just change the state - let the state machine handle ALL UI
        interactor.StateMachine.ChangeState(interactor.StateMachine.UIState);
    }
}