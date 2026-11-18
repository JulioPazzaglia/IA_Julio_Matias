using UnityEngine;

public class HunterStateMachine
{
    private StateMachine _stateMachine;
    private Hunter _hunter;

    public string CurrentStateName
    {
        get
        {
            if (_stateMachine.CurrentState is PatrolState)
                return "Patrol";
            if (_stateMachine.CurrentState is HuntState)
                return "Hunt";
            if (_stateMachine.CurrentState is GatherState)
                return "Gather";
            if (_stateMachine.CurrentState is RestState)
                return "Rest";
            return "Sin estado";
        }
    }

    public HunterStateMachine(Hunter hunter)
    {
        _hunter = hunter;
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(
            HunterStateType.Patrol,
            new PatrolState(_hunter, _stateMachine)
        );
        _stateMachine.RegisterState(HunterStateType.Hunt, new HuntState(_hunter, _stateMachine));
        _stateMachine.RegisterState(
            HunterStateType.Gather,
            new GatherState(_hunter, _stateMachine)
        );
        _stateMachine.RegisterState(HunterStateType.Rest, new RestState(_hunter, _stateMachine));
        _stateMachine.ChangeState(HunterStateType.Patrol);
    }

    public void Update()
    {
        _stateMachine.Update();
    }
}

public enum HunterStateType
{
    Patrol,
    Hunt,
    Gather,
    Rest,
}
