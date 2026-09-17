using UnityEngine;

public class HunterPatrolState : HunterState
{
    private Transform _waypointA;
    private Transform _waypointB;
    private Transform _currentWaypoint;
    private HunterPerception _perception;

    public HunterPatrolState(
    Hunter hunter,
    Transform waypointA,
    Transform waypointB,
    HunterPerception perception,
    StateMachine stateMachine
) : base(hunter, stateMachine)
    {
        _waypointA = waypointA;
        _waypointB = waypointB;
        _currentWaypoint = _waypointA;
        _perception = perception;
    }
    public override void Update()
    {
        Vector3 direction =
            _currentWaypoint.position - _hunter.transform.position;

        if (direction.magnitude < 1f)
        {
            _currentWaypoint =
                _currentWaypoint == _waypointA
                    ? _waypointB
                    : _waypointA;
        }

        Vector3 desired =
            direction.normalized * _hunter.MaxSpeed;

        _hunter.SetVelocity(desired);
        Boid detectedBoid = _perception.DetectBoid();

        if (detectedBoid != null)
        {
            _stateMachine.ChangeState(HunterStateType.Attack);
        }
    }
}