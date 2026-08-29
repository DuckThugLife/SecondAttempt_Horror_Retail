using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; // Required for Netcode namespaces


public class Interactor : NetworkBehaviour
{
    [Header("Classes")]
    [SerializeField] public PlayerStateMachine playerStateMachine;
    [SerializeField] public PlayerInputHandler playerInputHandler;
    [SerializeField] private Camera playerCamera;
    [SerializeField] public Transform objectHoldPoint;

    // Kept as standard property, synchronized via Target RPCs down below
    public GameObject heldObject { get; private set; }

    [Header("Settings")]
    [field: SerializeField] public float throwStrength { get; private set; } = 10f;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask;

    private IInteractable _currentInteractable;
    private IHoverable _currentHoverable;
    private bool _isLeaving = false;

    private void Update()
    {
        // Only allow input handling if this script belongs to the local player controlling it
        if (!IsOwner) return;

        HandleHover();

        if (playerInputHandler.LastKeyPressed == Key.E && !_isLeaving)
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

    public void SetLeaving()
    {
        _isLeaving = true;
        ClearHover();
    }


    public void AddHeldObject(GameObject _heldObject)
    {
        heldObject = _heldObject;

        // If this method is run on the server, force the targeted client to sync its variable
        if (IsServer)
        {
            SyncHeldObjectClientRpc(_heldObject.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    public void RemoveHeldObject()
    {
        heldObject = null;

        if (IsServer)
        {
            SyncHeldObjectClientRpc(ulong.MaxValue); // Special flag meaning null
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SyncHeldObjectClientRpc(ulong networkObjectId)
    {
        // If it's the null flag, empty the client reference
        if (networkObjectId == ulong.MaxValue)
        {
            heldObject = null;
            return;
        }

        // Find the matching game object on this local client machine and link it
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            heldObject = netObj.gameObject;
        }
    }
}
