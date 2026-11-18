using UnityEngine;

public class PatrolState : State
{
    private Hunter _hunter;
    private StateMachine _stateMachine;
    private Transform _currentWaypoint;
    private int _currentWaypointIndex;

    public PatrolState(Hunter hunter, StateMachine stateMachine)
    {
        _hunter = hunter;
        _stateMachine = stateMachine;
        _currentWaypointIndex = 0;
        _currentWaypoint = _hunter.WaypointA;
    }

    public override void Enter()
    {
        _hunter.SetVelocity(Vector3.zero);
        _hunter.SetTarget(null);
        _hunter.SetAction("Patrullando");
    }

    public override void Update()
    {
        _hunter.UpdateTrapTimer();
        _hunter.TryGenerateTrap();

        // Orden EXIGIDO: primero Boid muerto, luego Boid vivo si TBA termino.
        if (_hunter.Perception != null)
        {
            Boid deadBoid = _hunter.Perception.DetectDeadBoid();
            if (deadBoid != null)
            {
                _hunter.SetTarget(deadBoid);
                _stateMachine.ChangeState(HunterStateType.Gather);
                return;
            }

            if (_hunter.CanAttack && _hunter.Perception.DetectBoid() != null)
            {
                _stateMachine.ChangeState(HunterStateType.Hunt);
                return;
            }
        }

        if (_currentWaypoint == null)
        {
            _currentWaypoint = _hunter.WaypointA;
            if (_currentWaypoint == null)
            {
                _hunter.SetVelocity(Vector3.zero);
                return;
            }
        }

        Vector3 direction = _currentWaypoint.position - _hunter.transform.position;
        direction.y = 0f;
        if (direction.magnitude < _hunter.WaypointReachDistance)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % 4;
            if (_currentWaypointIndex == 0)
                _currentWaypoint = _hunter.WaypointA;
            else if (_currentWaypointIndex == 1)
                _currentWaypoint = _hunter.WaypointB;
            else if (_currentWaypointIndex == 2)
                _currentWaypoint = _hunter.WaypointC;
            else
                _currentWaypoint = _hunter.WaypointD;
        }

        if (_currentWaypoint != null)
            _hunter.SetSteering(Steering.Seek(_hunter, _currentWaypoint, _hunter.MaxSpeed));
        else
            _hunter.SetVelocity(Vector3.zero);
    }
}
