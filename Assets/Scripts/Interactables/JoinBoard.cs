using UnityEngine;

public class JoinBoard : MonoBehaviour, IInteractable, IHoverable
{
    private bool _isHovered;

    public void HoverEnter(Interactor interactor)
    {
        if (_isHovered) return;
        _isHovered = true;
        UIManager.Instance.HoverUI();
    }

    public void HoverExit(Interactor interactor)
    {
        if (!_isHovered) return;
        _isHovered = false;
        UIManager.Instance.UnHoverUI();
    }

    public void Interact(Interactor interactor)
    {
        if (!_isHovered) return;

        // Show the lobby UI
        UIManager.Instance.ShowLobbyUI();

        // Switch the player into UI state
        interactor.StateMachine.ChangeState(interactor.StateMachine.UIState);
    }
}