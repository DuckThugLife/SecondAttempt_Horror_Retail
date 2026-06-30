using UnityEngine;

public class ThrowableObject : MonoBehaviour, IInteractable, IHoverable
{
    private Transform originalParent;
    private bool _isHovered;
    private bool _isHeld;

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
        if (_isHeld) return;
        if (interactor.heldObject != null) return;

        interactor.AddHeldObject(gameObject);
        _isHeld = true;

        originalParent = gameObject.transform.parent;
        gameObject.transform.position = interactor.objectHoldPoint.position;
        gameObject.transform.SetParent(interactor.objectHoldPoint, true);
    }

    public void Use(Interactor interactor)
    {
        ThrowObject(interactor);
    }

    private void ThrowObject(Interactor interactor)
    {
        if (gameObject.GetComponent<Rigidbody>() == null) return;
        if (interactor == null) return;

        gameObject.transform.SetParent(originalParent);

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();

        Debug.Log("Throw object");
        Debug.Log(interactor.gameObject);
        Debug.Log(interactor.throwStrength);
        rb.AddRelativeForce(interactor.gameObject.transform.forward * interactor.throwStrength);

        _isHeld = false;
        interactor.RemoveHeldObject();
    }

  
}