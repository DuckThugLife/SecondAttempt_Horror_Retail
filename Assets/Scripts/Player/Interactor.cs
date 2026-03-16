using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("Classes")]
    [SerializeField] public PlayerStateMachine playerStateMachine;
    [SerializeField] public PlayerInputHandler playerInputHandler;
    [SerializeField] private Camera playerCamera;

    [Header("Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask;

    private IInteractable _currentInteractable;
    private IHoverable _currentHoverable;
    private bool _isLeaving = false; // bool to fix the hovering bug on leaving

    private void Awake()
    {

    }

    private void Update()
    {
        // Don't interact if settings is open
        if (UIManager.Instance.SessionUIManager.IsSettingsOpen())
            return;

        HandleHover();
        Debug.Log(playerInputHandler.LastKeyPressed);

        if (playerInputHandler.LastKeyPressed == Key.E && !_isLeaving) // Don't interact while leaving
        {
            _currentInteractable?.Interact(this);
            playerInputHandler.ResetLastKey();
        }
    }

    private void HandleHover()
    {
        // Skip hover processing if we're leaving
        if (_isLeaving) return;

        if (!Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                interactDistance,
                interactMask))
        {
            ClearHover();
            return;
        }

        var interactable = hit.collider.GetComponent<IInteractable>();
        var hoverable = hit.collider.GetComponent<IHoverable>();

        if (interactable == _currentInteractable)
            return;

        ClearHover();

        _currentInteractable = interactable;
        _currentHoverable = hoverable;

        _currentHoverable?.HoverEnter(this);
    }

    public void ClearHover()
    {
        if (_currentHoverable != null)
        {
            _currentHoverable.HoverExit(this);
            _currentHoverable = null;
        }

        _currentInteractable = null;
    }

    // Call this before leaving to prevent re-hover
    public void SetLeaving()
    {
        _isLeaving = true;
        ClearHover();
    }
}