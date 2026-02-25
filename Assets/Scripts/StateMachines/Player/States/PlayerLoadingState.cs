using Unity.Services.Multiplayer;
using UnityEngine.SceneManagement;

public class PlayerLoadingState : PlayerState
{
    public PlayerLoadingState(PlayerStateMachine machine) : base(machine)
    {
        // Subscribe in constructor
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionJoined += OnSessionJoined;
            SessionManager.Instance.OnSessionCreated += OnSessionJoined;
        }
    }

    private void OnSessionJoined(ISession session)
    {
        // When session is established, leave loading state
        if (stateMachine.GetCurrentState() == this)
        {
            if (SceneManager.GetActiveScene().name == "GameScene")
                stateMachine.ChangeState(stateMachine.AliveState);
            else
                stateMachine.ChangeState(stateMachine.LobbyState);
        }
    }

    public override void Enter()
    {
        stateMachine.PlayerInputHandler.SetMovementEnabled(false);
        stateMachine.UnlockCursor();
        UIManager.Instance.SessionUIManager.ShowLoading(true);
        UIManager.Instance.SessionUIManager.HideLobbyUI();
    }

    // unsubscribe
    ~PlayerLoadingState()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionJoined -= OnSessionJoined;
            SessionManager.Instance.OnSessionCreated -= OnSessionJoined;
        }
    }
}