using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("Classes")]
    [SerializeField] public PlayerStateMachine playerStateMachine;
    [SerializeField] public PlayerInputHandler playerInputHandler;
    [SerializeField] private Camera playerCamera;
    [SerializeField] public Transform objectHoldPoint;
    public GameObject heldObject { get; private set; }

    [Header("Settings")]

    [field: SerializeField] public float throwStrength { get; private set; } = 10f;
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

        HandleHover();

        if (playerInputHandler.LastKeyPressed == Key.E && !_isLeaving) // Don't interact while leaving
        {
            _currentInteractable?.Interact(this);
            playerInputHandler.ResetLastKey();
        }

        if (heldObject != null && heldObject.TryGetComponent<IInteractable>(out var interactable) && playerInputHandler.LeftClickPressed)
        {
            interactable.Use(this);
            playerInputHandler.ResetLeftClick();
        }
        else if (playerInputHandler.LeftClickPressed)
        {
            _currentInteractable?.Use(this);
            playerInputHandler.ResetLeftClick();
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

    public void AddHeldObject(GameObject _heldObject)
    {
        heldObject = _heldObject;
    }
    public void RemoveHeldObject()
    {
        heldObject = null;
    }
}