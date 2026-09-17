using UnityEngine;

public class HunterStateMachine
{
    private StateMachine _stateMachine;
    private Hunter _hunter;

    public HunterStateMachine(Hunter hunter)
    {
        _hunter = hunter;

        _stateMachine = new StateMachine();

        _stateMachine.RegisterState(
            HunterStateType.Patrol,
            new PatrolState(_hunter, _stateMachine)
        );

        _stateMachine.RegisterState(
            HunterStateType.Attack,
            new AttackState(_hunter, _stateMachine)
        );

        _stateMachine.ChangeState(HunterStateType.Patrol);
    }

    public void Update()
    {
        _stateMachine.Update();
    }

    private class PatrolState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;
        private Transform _currentWaypoint;

        public PatrolState(
            Hunter hunter,
            StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;

            _currentWaypoint = _hunter.WaypointA;
        }

        public override void Update()
        {
            Vector3 direction =
                _currentWaypoint.position - _hunter.transform.position;

            if (direction.magnitude < 1f)
            {
                _currentWaypoint =
                    _currentWaypoint == _hunter.WaypointA
                        ? _hunter.WaypointB
                        : _hunter.WaypointA;
            }

            Vector3 steering = Steering.Seek(
                _hunter,
                _currentWaypoint,
                _hunter.MaxSpeed
            );

            _hunter.SetSteering(steering);

            Boid detectedBoid = _hunter.Perception.DetectBoid();

            if (detectedBoid != null && _hunter.CanAttack)
            {
                _hunter.SetTarget(detectedBoid);

                _stateMachine.ChangeState(
                    HunterStateType.Attack
                );
            }
        }
    }

    private class AttackState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;

        public AttackState(
            Hunter hunter,
            StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;
        }

        public override void Update()
        {
            if (_hunter.Target == null)
            {
                _stateMachine.ChangeState(
                    HunterStateType.Patrol
                );

                return;
            }

            float distance = Vector3.Distance(
                _hunter.transform.position,
                _hunter.Target.transform.position
            );

            if (distance <= _hunter.MeleeAttackRadius)
            {
                _hunter.MeleeAttack();
                _stateMachine.ChangeState(
                    HunterStateType.Patrol
                );

                return;
            }

            if (distance <= _hunter.RangeAttackRadius
                && _hunter.CanAttack)
            {
                _hunter.RangedAttack();
                _stateMachine.ChangeState(
                    HunterStateType.Patrol
                );

                return;
            }

            Vector3 steering = Steering.Pursuit(
                _hunter,
                _hunter.Target,
                _hunter.MaxSpeed
            );

            _hunter.SetSteering(steering);
        }
    }
}

public enum HunterStateType
{
    Patrol,
    Attack,
    Gather
}