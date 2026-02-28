using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerStateMachine : NetworkBehaviour
{
    private Stack<PlayerState> _stateStack = new Stack<PlayerState>();

    [SerializeField] public PlayerController PlayerController;
    [SerializeField] public PlayerInputHandler PlayerInputHandler;
    [SerializeField] public Interactor Interactor;

    public static PlayerStateMachine LocalInstance { get; private set; }

    // Cached states (no allocations during gameplay)
    public PlayerLobbyState LobbyState { get; private set; }
    public PlayerAliveState AliveState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }
    public LobbyMenuState LobbyMenuState { get; private set; }
    public PlayerLoadingState LoadingState { get; private set; }
    public ChatState ChatState { get; private set; }

    public PlayerState CurrentState => _stateStack.Count > 0 ? _stateStack.Peek() : null;

    private void Awake()
    {
        LobbyState = new PlayerLobbyState(this);
        AliveState = new PlayerAliveState(this);
        DeadState = new PlayerDeadState(this);
        LobbyMenuState = new LobbyMenuState(this);
        LoadingState = new PlayerLoadingState(this);
        ChatState = new ChatState(this);
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

        HandleGlobalInput();
        CurrentState?.Tick();
    }

    public void PushState(PlayerState newState)
    {
        if (_stateStack.Count > 0)
            _stateStack.Peek().Exit();

        _stateStack.Push(newState);
        newState.Enter();
        Debug.Log($"Pushed state: {newState.GetType().Name}");
    }

    public void PopState()
    {
        if (_stateStack.Count <= 1) return;

        var current = _stateStack.Pop();
        current.Exit();

        _stateStack.Peek().Enter();
        Debug.Log($"Popped state, current: {_stateStack.Peek().GetType().Name}");
    }

    public void ChangeState(PlayerState newState)
    {
        while (_stateStack.Count > 0)
            _stateStack.Pop().Exit();

        _stateStack.Push(newState);
        newState.Enter();
        Debug.Log($"Changed to state: {newState.GetType().Name}");
    }

    public void RevertToPreviousState()
    {
        // Not needed with stack
    }

    public PlayerState GetCurrentState() => CurrentState;

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void SetPlayerEnabled(bool enabled)
    {
        PlayerInputHandler.SetMovementEnabled(enabled);

        if (enabled)
        {
            PlayerController.EnableTurning();
            LockCursor();
        }
        else
        {
            PlayerController.DisableTurning();
            UnlockCursor();
        }
    }

    public void OpenLobbyMenu()
    {
        PushState(LobbyMenuState);
    }

    private void HandleGlobalInput()
    {
        if (PlayerInputHandler.LastKeyPressed == Key.Enter)
        {
            if (!(CurrentState is BaseUIState))
            {
                PushState(ChatState);
            }
            PlayerInputHandler.ResetLastKey();
        }
    }
}