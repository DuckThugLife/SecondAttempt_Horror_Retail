using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkThrowableObject : NetworkBehaviour, IInteractable, IHoverable
{
    [field: SerializeField] public MeshRenderer modelMeshRenderer { get; private set; }

    // -1 means on the ground (physics active). Otherwise stores the true LocalClientId of the holder.
    private NetworkVariable<long> _holdingPlayerId = new NetworkVariable<long>(-1);
    private bool _isHovered;
    private Rigidbody rb;
    private Transform targetHoldPoint;

    // --- Smooth Visual Settlement Correction Pass ---
    private bool _isCorrectingPosition = false;
    private Vector3 _targetCorrectPos;
    private Quaternion _targetCorrectRot;
    [SerializeField] private float _correctionBlendDuration = 0.3f;
    private float _correctionTimeElapsed = 0f;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        _holdingPlayerId.OnValueChanged += OnHoldingPlayerChanged;

        // Late-join catch-up: If a player joins late while someone is actively holding it, lock it to their hand
        if (_holdingPlayerId.Value != -1)
        {
            OnHoldingPlayerChanged(-1, _holdingPlayerId.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _holdingPlayerId.OnValueChanged -= OnHoldingPlayerChanged;
    }

    private void Update()
    {
        // Smoothly glide to the final server resting position when it stops moving
        if (_isCorrectingPosition)
        {
            _correctionTimeElapsed += Time.deltaTime;
            float t = _correctionTimeElapsed / _correctionBlendDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(transform.position, _targetCorrectPos, smoothT);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetCorrectRot, smoothT);

            if (t >= 1.0f) _isCorrectingPosition = false;
        }
    }

    private void FixedUpdate()
    {
        // ONLY the server checks if the rolling object has naturally come to a rest
        if (!IsServer) return;

        if (_holdingPlayerId.Value == -1 && rb != null && !rb.isKinematic)
        {
            // Check if the object has practically stopped moving (Unity 6 linear velocity properties)
            if (rb.linearVelocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f)
            {
                rb.isKinematic = true; // Lock physics on the server to prevent micro-sliding

                // Server reclaims absolute network authority over the resting object
                if (!GetComponent<NetworkObject>().IsOwnedByServer)
                {
                    GetComponent<NetworkObject>().RemoveOwnership();
                }

                // Tell all clients to smoothly ease their local cubes to this exact landing coordinate
                BlendToFinalRestingPositionClientRpc(transform.position, transform.rotation);
            }
        }
    }

    [Rpc(SendTo.NotServer)]
    private void BlendToFinalRestingPositionClientRpc(Vector3 serverPosition, Quaternion serverRotation)
    {
        if (rb != null) rb.isKinematic = true; // Freeze local physics so it doesn't fight the blend

        _targetCorrectPos = serverPosition;
        _targetCorrectRot = serverRotation;
        _correctionTimeElapsed = 0f;
        _isCorrectingPosition = true;
    }

    private void LateUpdate()
    {
        // Smoothly lock the object to the interactor's hands locally if held
        if (!_isCorrectingPosition && targetHoldPoint != null)
        {
            Bounds myBounds = modelMeshRenderer.bounds;
            Vector3 offset = new Vector3(myBounds.extents.x, myBounds.extents.y, myBounds.extents.z);
            transform.position = targetHoldPoint.TransformPoint(offset);
            transform.rotation = targetHoldPoint.rotation;
        }
    }

    private void OnHoldingPlayerChanged(long oldPlayerId, long newPlayerId)
    {
        if (newPlayerId == -1)
        {
            // Object released! Clear tracking references. 
            // The individual client throw execution RPCs will handle waking up physics.
            targetHoldPoint = null;
            _isCorrectingPosition = false;
        }
        else
        {
            _isCorrectingPosition = false;

            // Freeze physics behavior while being held in a player's hand
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            // Map the target transform hold point via your player collection entities
            foreach (var spawnedObject in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                if (spawnedObject.IsPlayerObject && spawnedObject.OwnerClientId == (ulong)newPlayerId)
                {
                    Interactor interactor = spawnedObject.GetComponentInChildren<Interactor>();
                    if (interactor != null)
                    {
                        targetHoldPoint = interactor.objectHoldPoint;
                        break;
                    }
                }
            }
        }
    }

    public void Interact(Interactor interactor)
    {
        if (_holdingPlayerId.Value != -1 || _isCorrectingPosition) return;
        RequestPickupRpc(NetworkManager.Singleton.LocalClientId);
    }

    public void Use(Interactor interactor)
    {
        if (_holdingPlayerId.Value != (long)NetworkManager.Singleton.LocalClientId) return;

        Vector3 throwDirection = interactor.objectHoldPoint.transform.forward;
        float force = interactor.throwStrength;
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        // Route the throw request to the server immediately
        RequestThrowServerRpc(localClientId, throwDirection, force);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPickupRpc(ulong interactorClientId)
    {
        if (_holdingPlayerId.Value != -1) return;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _isCorrectingPosition = false;

        // Keep the main object under server ownership to allow any anonymous client to touch it
        if (!GetComponent<NetworkObject>().IsOwnedByServer)
        {
            GetComponent<NetworkObject>().RemoveOwnership();
        }

        _holdingPlayerId.Value = (long)interactorClientId;
        UpdatePlayerHeldList(interactorClientId, true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestThrowServerRpc(ulong interactorClientId, Vector3 throwDir, float throwForce)
    {
        if (_holdingPlayerId.Value != (long)interactorClientId) return;

        // Disconnect inventory references
        _holdingPlayerId.Value = -1;
        UpdatePlayerHeldList(interactorClientId, false);

        // 1. Run the real physical throw launch natively on the Server node
        ExecutePhysicalThrow(throwDir, throwForce);

        // 2. Broadcast the EXACT SAME throw parameters down to every client machine simultaneously
        ExecutePhysicalThrowClientRpc(throwDir, throwForce);
    }

    [Rpc(SendTo.NotServer)]
    private void ExecutePhysicalThrowClientRpc(Vector3 throwDir, float throwForce)
    {
        // 3. Every client machine launches their local physical instance of the cube in their own world view
        ExecutePhysicalThrow(throwDir, throwForce);
    }

    private void ExecutePhysicalThrow(Vector3 throwDir, float throwForce)
    {
        targetHoldPoint = null;
        _isCorrectingPosition = false;

        if (rb != null)
        {
            // CRITICAL ORDER OF OPERATIONS: Grouping these into a single frame execution pass
            // forces Unity to wake up the rigidbody out of its resting state, turn on collisions,
            // and apply the force impulse cleanly without freezing or dropping the calculations.
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.WakeUp();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwDir * throwForce, ForceMode.Impulse);
        }
    }

    private void UpdatePlayerHeldList(ulong clientId, bool isAdding)
    {
        foreach (var spawnedObject in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (spawnedObject.IsPlayerObject && spawnedObject.OwnerClientId == clientId)
            {
                Interactor interactor = spawnedObject.GetComponentInChildren<Interactor>();
                if (interactor != null)
                {
                    if (isAdding) interactor.AddHeldObject(gameObject);
                    else interactor.RemoveHeldObject();
                }
                break;
            }
        }
    }

    public void HoverEnter(Interactor interactor) { if (_holdingPlayerId.Value == -1 && !_isCorrectingPosition) UIManager.Instance.GameUIManager.HoverUI(); }
    public void HoverExit(Interactor interactor) { UIManager.Instance.GameUIManager.UnHoverUI(); }
}
