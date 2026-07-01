using UnityEngine;

public class ThrowableObject : MonoBehaviour, IInteractable, IHoverable
{
    [field: SerializeField] public MeshRenderer modelMeshRenderer { get; private set; }
    private Transform originalParent;
    private Rigidbody rb;
    private bool _isHovered;
    private bool _isHeld;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

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
        PickupObject(interactor);
    }

    public void Use(Interactor interactor)
    {
        ThrowObject(interactor);
    }

    private void PickupObject(Interactor interactor)
    {
        if (!_isHovered) return;
        if (_isHeld) return;
        if (interactor.heldObject != null) return;
        _isHeld = true;
        originalParent = gameObject.transform.parent;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = true; 
        }


        GetBoundsAndOffset(interactor);
        interactor.AddHeldObject(gameObject);
        
    }


    private void ThrowObject(Interactor interactor)
    {
        if (rb == null) return;
        if (interactor == null) return;

        gameObject.transform.SetParent(originalParent);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        Debug.Log("Throw object");
        Debug.Log(interactor.gameObject);
        Debug.Log(interactor.throwStrength);
        rb.AddForce(interactor.objectHoldPoint.transform.forward * interactor.throwStrength);

        _isHeld = false;
        interactor.RemoveHeldObject();
    }

    private void GetBoundsAndOffset(Interactor interactor)
    {
        transform.SetParent(interactor.objectHoldPoint, false);
        Bounds myBounds = modelMeshRenderer.bounds;
        Vector3 offset = new Vector3(myBounds.extents.x, myBounds.extents.y, myBounds.extents.z);

        transform.localPosition = offset;
    }


}