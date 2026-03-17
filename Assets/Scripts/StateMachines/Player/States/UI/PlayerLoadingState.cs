using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLoadingState : PlayerState
{
    private float _joinStartTime;
    private const float JoinTimeout = 20f; // 20 second timeout

    public PlayerLoadingState(PlayerStateMachine machine) : base(machine)
    {
        if (SessionManager.Instance != null)
        {
            // Subscribe to session changes
            SessionManager.Instance.OnSessionChanged += OnSessionChanged;
            // Still keep host disconnect if you need it
            SessionManager.Instance.OnHostDisconnected += OnHostDisconnected;
        }
    }

    public override void Enter()
    {
        _joinStartTime = Time.time;
        stateMachine.PlayerInputHandler.SetMovementEnabled(false);
        stateMachine.UnlockCursor();
        UIManager.Instance.SessionUIManager.ShowLoading(true);
        UIManager.Instance.SessionUIManager.HideLobbyUI();
    }

    private void OnHostDisconnected()
    {
        if (stateMachine.GetCurrentState() == this)
        {
            Debug.Log("Host disconnected during join - returning to lobby");
            stateMachine.ChangeState(stateMachine.LobbyState);
        }
    }

    private void OnSessionChanged(ISession session)
    {
        if (stateMachine.GetCurrentState() != this)
            return;

        // If session is null, ignore (we're leaving, not joining)
        if (session == null)
            return;

        if (SceneManager.GetActiveScene().name == "GameScene")
            stateMachine.ChangeState(stateMachine.GameState);
        else
            stateMachine.ChangeState(stateMachine.LobbyState);
    }

    public override void Tick()
    {
        // Timeout if join takes too long
        if (Time.time - _joinStartTime > JoinTimeout)
        {
            Debug.Log("Join timeout - returning to lobby");
            stateMachine.ChangeState(stateMachine.LobbyState);
        }
    }
}