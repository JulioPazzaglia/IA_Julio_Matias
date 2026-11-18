using UnityEngine;

public class FlockingState : State
{
    private Boid _boid;
    private StateMachine _stateMachine;

    public FlockingState(Boid boid, StateMachine stateMachine)
    {
        _boid = boid;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _boid.SetDefaultColor();
    }

    public override void Update()
    {
        if (_boid.Perception == null)
            return;

        Trap trap = _boid.Perception.DetectTrap();
        if (trap != null)
        {
            _boid.SetTargetTrap(trap);
            _stateMachine.ChangeState(BoidStateType.SeekObject);
            return;
        }
        _boid.SetSteering(_boid.CalculateFlocking());
    }
}
