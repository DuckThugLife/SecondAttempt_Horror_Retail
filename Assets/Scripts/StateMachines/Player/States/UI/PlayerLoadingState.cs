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
            SessionManager.Instance.OnSessionJoined += OnSessionJoined;
            SessionManager.Instance.OnSessionCreated += OnSessionJoined;
            SessionManager.Instance.OnHostDisconnected += OnHostDisconnected; // Add this event
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

    private void OnSessionJoined(ISession session)
    {
        if (stateMachine.GetCurrentState() == this)
        {
            if (SceneManager.GetActiveScene().name == "GameScene")
                stateMachine.ChangeState(stateMachine.AliveState);
            else
                stateMachine.ChangeState(stateMachine.LobbyState);
        }
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