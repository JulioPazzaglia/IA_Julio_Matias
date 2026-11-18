using UnityEngine;

public class HuntState : State
{
    private Hunter _hunter;
    private StateMachine _stateMachine;
    private Vector3 _lastKnownPosition;

    public HuntState(Hunter hunter, StateMachine stateMachine)
    {
        _hunter = hunter;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _hunter.SetAction("Buscando presa en Hunt");
        _lastKnownPosition = _hunter.transform.position;
        Boid candidate = _hunter.Perception != null ? _hunter.Perception.DetectBoid() : null;
        _hunter.SetTarget(candidate);
        if (candidate != null)
            _lastKnownPosition = candidate.transform.position;
    }

    public override void Update()
    {
        _hunter.ConsumeEnergy();
        if (_hunter.Energy <= 0f)
        {
            _stateMachine.ChangeState(HunterStateType.Rest);
            return;
        }

        Boid target = _hunter.Target;
        if (target != null && target.IsAlive && target.LeftBoundsSinceTargeted)
        {
            _stateMachine.ChangeState(HunterStateType.Patrol);
            return;
        }
        Boid deadBoid = _hunter.Perception != null ? _hunter.Perception.DetectDeadBoid() : null;
        if (deadBoid != null)
        {
            _hunter.SetTarget(deadBoid);
            _stateMachine.ChangeState(HunterStateType.Gather);
            return;
        }

        if (target == null || !target.isActiveAndEnabled || !target.IsAlive || target.IsRespawning)
        {
            Boid replacement = _hunter.Perception != null ? _hunter.Perception.DetectBoid() : null;
            _hunter.SetTarget(replacement);
            target = replacement;
            if (target == null)
            {
                MoveToLastKnownPosition();
                return;
            }
        }
        _lastKnownPosition = target.transform.position;
        Vector3 delta = target.transform.position - _hunter.transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        if (_hunter.CanAttack && distance <= _hunter.MeleeAttackRadius)
        {
            _hunter.MeleeAttack();
            return; 
        }
        if (_hunter.CanAttack && distance <= _hunter.RangeAttackRadius)
        {
            _hunter.RangedAttack(); 
            return; 
        }

        _hunter.SetAction("Persiguiendo con Pursuit");
        _hunter.SetSteering(Steering.Pursuit(_hunter, target, _hunter.MaxSpeed));
    }

    private void MoveToLastKnownPosition()
    {
        Vector3 difference = _lastKnownPosition - _hunter.transform.position;
        difference.y = 0f;
        if (
            difference.sqrMagnitude
            <= _hunter.LastPositionReachDistance * _hunter.LastPositionReachDistance
        )
        {
            _hunter.SetVelocity(Vector3.zero);
            _hunter.SetAction("Hunt: esperando en ultima posicion conocida");
            return;
        }
        _hunter.SetAction("Hunt: hacia ultima posicion conocida");
        Vector3 desired = difference.normalized * _hunter.MaxSpeed;
        _hunter.SetSteering(desired - _hunter.Velocity);
    }
}
