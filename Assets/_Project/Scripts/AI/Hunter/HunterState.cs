public abstract class HunterState : State
{
    protected Hunter _hunter;
    protected StateMachine _stateMachine;

    protected HunterState(Hunter hunter, StateMachine stateMachine)
    {
        _hunter = hunter;
        _stateMachine = stateMachine;
    }
}