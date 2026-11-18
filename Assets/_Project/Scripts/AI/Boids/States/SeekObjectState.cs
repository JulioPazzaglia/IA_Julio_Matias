using UnityEngine;

public class SeekObjectState : State
{
    private Boid _boid;
    private StateMachine _stateMachine;

    public SeekObjectState(Boid boid, StateMachine stateMachine)
    {
        _boid = boid;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _boid.SetStateColor(Color.yellow);
    }

    public override void Update()
    {
        Trap trap = _boid.TargetTrap;
        if (
            trap == null
            || !trap.isActiveAndEnabled
            || !trap.IsActive
            || trap.IsOccupied
            || (
                _boid.Perception != null && !_boid.Perception.InVisionRange(trap.transform.position)
            )
        )
        {
            _boid.ClearTargetTrap();
            _stateMachine.ChangeState(BoidStateType.Flocking);
            return;
        }

        Vector3 offset = trap.transform.position - _boid.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        float stopDistance = _boid.GetTrapStopDistance(trap);

        if (distance <= stopDistance + _boid.ObjectReachTolerance)
        {
            _boid.SetVelocity(Vector3.zero);
            if (trap.Occupy(_boid))
                _stateMachine.ChangeState(BoidStateType.Consume);
            else
            {
                _boid.ClearTargetTrap();
                _stateMachine.ChangeState(BoidStateType.Flocking);
            }
            return;
        }

        // Arrive; la barrera de distancia y PreventOverlaps impiden pisar el objeto.
        float speed =
            _boid.MaxSpeed * Mathf.Clamp01((distance - stopDistance) / _boid.ObjectSlowingDistance);
        Vector3 steering = Steering.Arrive(_boid, trap.transform, speed, stopDistance);
        _boid.SetSteering(steering + _boid.CalculateEvadeSeparation());
    }
}
