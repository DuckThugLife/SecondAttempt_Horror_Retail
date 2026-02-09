public abstract class PlayerState
{
    protected readonly PlayerStateMachine stateMachine;

    protected PlayerState(PlayerStateMachine machine)
    {
        stateMachine = machine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick() { }
}