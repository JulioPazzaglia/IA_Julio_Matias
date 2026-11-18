using UnityEngine;

public class BoidStateMachine
{
    private StateMachine _stateMachine;
    private Boid _boid;

    public string CurrentStateName
    {
        get
        {
            if (_stateMachine.CurrentState is FlockingState)
                return "Flocking";
            if (_stateMachine.CurrentState is EvadeState)
                return "Evade";
            if (_stateMachine.CurrentState is SeekObjectState)
                return "Seek Object";
            if (_stateMachine.CurrentState is ConsumeState)
                return "Consume";
            if (_stateMachine.CurrentState is DeadState)
                return "Dead";
            if (_stateMachine.CurrentState is RespawnState)
                return "Respawn";
            return "Sin estado";
        }
    }

    public BoidStateMachine(Boid boid)
    {
        _boid = boid;
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(
            BoidStateType.Flocking,
            new FlockingState(_boid, _stateMachine)
        );
        _stateMachine.RegisterState(BoidStateType.Evade, new EvadeState(_boid, _stateMachine));
        _stateMachine.RegisterState(
            BoidStateType.SeekObject,
            new SeekObjectState(_boid, _stateMachine)
        );
        _stateMachine.RegisterState(BoidStateType.Consume, new ConsumeState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.Dead, new DeadState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.Respawn, new RespawnState(_boid, _stateMachine));
        _stateMachine.ChangeState(BoidStateType.Flocking);
    }

    public void Update()
    {
        // Prioridades del recuperatorio: Dead > Evade > Consume > Seek Object > Flocking.
        // Las transiciones se evalúan en la propia FSM.
        if (
            !_boid.IsAlive
            && !_boid.IsRespawning
            && !(_stateMachine.CurrentState is DeadState)
            && !(_stateMachine.CurrentState is RespawnState)
        )
        {
            _stateMachine.ChangeState(BoidStateType.Dead);
        }
        else if (
            _boid.IsAlive
            && _boid.Perception != null
            && _boid.Perception.DetectHunter() != null
            && !(_stateMachine.CurrentState is EvadeState)
        )
        {
            _stateMachine.ChangeState(BoidStateType.Evade);
        }
        _stateMachine.Update();
    }
}

public enum BoidStateType
{
    Flocking,
    Evade,
    SeekObject,
    Consume,
    Dead,
    Respawn,
}
