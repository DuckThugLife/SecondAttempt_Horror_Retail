using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class PlayerStateMachine : NetworkBehaviour
{
    //  track states
    private PlayerState _currentState;
    private PlayerState _previousState; 

    [SerializeField] public PlayerController PlayerController;
    [SerializeField] public PlayerInputHandler PlayerInputHandler;

    public static PlayerStateMachine LocalInstance { get; private set; }




    // Cached states (no allocations during gameplay)
    public PlayerLobbyState LobbyState { get; private set; }
    public PlayerAliveState AliveState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }
    public PlayerUIState UIState { get; private set; }
    public PlayerLoadingState LoadingState { get; private set; }

    private void Awake()
    {
        LobbyState = new PlayerLobbyState(this);
        AliveState = new PlayerAliveState(this);
        DeadState = new PlayerDeadState(this);
        UIState = new PlayerUIState(this);
        LoadingState = new PlayerLoadingState(this);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            ChangeState(LobbyState);
        }
    }

    public override void OnDestroy()
    {
        if (IsOwner && LocalInstance == this)
            LocalInstance = null;
    }

    private void Update()
    {
        if (!IsOwner) return;
        _currentState?.Tick();
    }

    public void ChangeState(PlayerState newState)
    {
        if (_currentState == newState) return;
        Debug.Log($"State changing from {_currentState?.GetType().Name} to {newState.GetType().Name}");

        _currentState?.Exit();
       
        _previousState = _currentState;
        _currentState = newState;
        _currentState.Enter();
    }

    // Go back to last state
    public void RevertToPreviousState()
    {
        if (_previousState != null)
            ChangeState(_previousState);
    }

    public PlayerState GetCurrentState()
    {
        return _currentState;
    }


    // Lock & hide the cursor for gameplay
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Unlock & show the cursor for UI
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }



}