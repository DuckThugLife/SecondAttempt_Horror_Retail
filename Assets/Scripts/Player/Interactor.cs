using UnityEngine;

public class Interactor : MonoBehaviour
{
    [Header("Classes")]
    [HideInInspector] public PlayerStateMachine StateMachine;
    [SerializeField] private Camera playerCamera;

    [Header("Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask;

    private IInteractable _currentInteractable;
    private IHoverable _currentHoverable;



    private void Awake()
    {
        if (StateMachine == null)
            StateMachine = GetComponent<PlayerStateMachine>();
    }


    private void Update()
    {
        HandleHover();

        if (Input.GetKeyDown(KeyCode.E))
        {
            _currentInteractable?.Interact(this);
        }
    }

    private void HandleHover()
    {
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

    private void ClearHover()
    {
        if (_currentHoverable != null)
        {
            _currentHoverable.HoverExit(this);
            _currentHoverable = null;
        }

        _currentInteractable = null;
    }
}
