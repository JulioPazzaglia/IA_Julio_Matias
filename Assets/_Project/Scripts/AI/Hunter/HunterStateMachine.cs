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

        _stateMachine.RegisterState(
            HunterStateType.Gather,
            new GatherState(_hunter, _stateMachine)
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
        private int _currentWaypointIndex;

        public PatrolState(Hunter hunter, StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;

            _currentWaypointIndex = 0;
            _currentWaypoint = _hunter.WaypointA;
        }

        public override void Update()
        {
            _hunter.UpdateTrapTimer();
            _hunter.TryGenerateTrap();

            Vector3 direction = _currentWaypoint.position - _hunter.transform.position;

            if (direction.magnitude < 1f)
            {
                _currentWaypointIndex++;

                if (_currentWaypointIndex > 3)
                    _currentWaypointIndex = 0;

                if (_currentWaypointIndex == 0)
                    _currentWaypoint = _hunter.WaypointA;
                else if (_currentWaypointIndex == 1)
                    _currentWaypoint = _hunter.WaypointB;
                else if (_currentWaypointIndex == 2)
                    _currentWaypoint = _hunter.WaypointC;
                else if (_currentWaypointIndex == 3)
                    _currentWaypoint = _hunter.WaypointD;
            }

            Vector3 steering = Steering.Seek(_hunter, _currentWaypoint, _hunter.MaxSpeed);

            _hunter.SetSteering(steering);

            Boid detectedBoid = _hunter.Perception.DetectBoid();

            if (detectedBoid != null && _hunter.CanAttack)
            {
                _hunter.SetTarget(detectedBoid);

                _stateMachine.ChangeState(HunterStateType.Attack);

                return;
            }

            Boid deadBoid = _hunter.Perception.DetectDeadBoid();

            if (deadBoid != null)
            {
                _hunter.SetTarget(deadBoid);

                Debug.Log("[HUNTER] Detectó un Boid muerto. Entrando en Gather.");

                _stateMachine.ChangeState(HunterStateType.Gather);
            }
        }
    }

    private class AttackState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;

        public AttackState(Hunter hunter, StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;
        }

        public override void Update()
        {
            if (_hunter.Target == null)
            {
                _stateMachine.ChangeState(HunterStateType.Patrol);

                return;
            }

            if (!_hunter.Perception.InRange(_hunter.Target.transform.position))
            {
                Debug.Log("[HUNTER] Perdió al Boid. Volviendo a Patrol.");

                _stateMachine.ChangeState(HunterStateType.Patrol);

                return;
            }

            float distance = Vector3.Distance(
                _hunter.transform.position,
                _hunter.Target.transform.position
            );
            if (_hunter.Target.TargetTrap != null && _hunter.Target.TargetTrap.IsOccupied)
            {
                if (distance <= _hunter.MeleeAttackRadius)
                {
                    _hunter.MeleeAttack();

                    _stateMachine.ChangeState(HunterStateType.Gather);

                    return;
                }

                Vector3 persuitSteering = Steering.Pursuit(_hunter, _hunter.Target, _hunter.MaxSpeed);

                _hunter.SetSteering(persuitSteering);

                return;
            }
            if (distance <= _hunter.MeleeAttackRadius)
            {
                _hunter.MeleeAttack();

                _stateMachine.ChangeState(HunterStateType.Patrol);

                return;
            }

            if (distance <= _hunter.RangeAttackRadius && _hunter.CanAttack)
            {
                _hunter.RangedAttack();

                _stateMachine.ChangeState(HunterStateType.Patrol);

                return;
            }

            Vector3 steering = Steering.Pursuit(_hunter, _hunter.Target, _hunter.MaxSpeed);

            _hunter.SetSteering(steering);
        }
    }

    private class GatherState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;

        private float _gatherTimer;
        private float _gatherDuration = 2f;

        public GatherState(Hunter hunter, StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;
        }
        public override void Enter()
        {
            _gatherTimer = 0f;
        }
        public override void Update()
        {
            if (_hunter.Target == null)
            {
                _stateMachine.ChangeState(HunterStateType.Patrol);

                return;
            }

            float distance = Vector3.Distance(
                _hunter.transform.position,
                _hunter.Target.transform.position
            );

            if (distance <= 1f)
            {
                _hunter.SetVelocity(Vector3.zero);

                _gatherTimer += Time.deltaTime;

                if (_gatherTimer >= _gatherDuration)
                {
                    Debug.Log(
                        "[HUNTER] Terminó de recoger al Boid."
                    );

                    Trap trap = _hunter.Target.TargetTrap;

                    _hunter.Target.StartRespawn();

                    _hunter.RemoveTrap(trap);

                    _hunter.ResetTrapTimer();

                    _hunter.Target.ClearTargetTrap();

                    _stateMachine.ChangeState(
                        HunterStateType.Patrol
                    );
                }

                return;
            }

            Vector3 steering = Steering.Arrive(
                _hunter,
                _hunter.Target.transform,
                _hunter.MaxSpeed,
                2f
            );

            _hunter.SetSteering(steering);
        }
    }
}

public enum HunterStateType
{
    Patrol,
    Attack,
    Gather,
}
