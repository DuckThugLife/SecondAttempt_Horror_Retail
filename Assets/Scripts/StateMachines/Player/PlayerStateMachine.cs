using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

public class PlayerStateMachine : NetworkBehaviour
{
    private PlayerState _currentState;

    [SerializeField] public PlayerController PlayerController;
    [SerializeField] public PlayerInputHandler PlayerInputHandler;
   



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
            ChangeState(LobbyState);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        _currentState?.Tick();
    }

    public void ChangeState(PlayerState newState)
    {
        if (_currentState == newState) return;

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }
}