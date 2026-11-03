using UnityEngine;

public class HunterStateMachine
{
    private StateMachine _stateMachine;
    private Hunter _hunter;

    public string CurrentStateName => _stateMachine.CurrentState != null
        ? _stateMachine.CurrentState.GetType().Name : "Sin estado";

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
            HunterStateType.Chase,
            new ChaseState(_hunter, _stateMachine)
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

    // Estados de la FSM (se mantienen en este archivo para respetar la estructura original).

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

        public override void Enter()
        {
            _hunter.SetAction("Patrullando");
        }

        public override void Update()
        {
            _hunter.UpdateTrapTimer();
            _hunter.TryGenerateTrap();

            // Los boids muertos tienen prioridad para no dejar cuerpos sin recoger.
            if (_hunter.Perception != null)
            {
                Boid deadBoid = _hunter.Perception.DetectDeadBoid();
                if (deadBoid != null)
                {
                    _hunter.SetTarget(deadBoid);
                    _stateMachine.ChangeState(HunterStateType.Gather);
                    return;
                }

                Boid detectedBoid = _hunter.Perception.DetectBoid();
                if (detectedBoid != null && _hunter.CanAttack)
                {
                    _hunter.SetTarget(detectedBoid);
                    _stateMachine.ChangeState(HunterStateType.Attack);
                    return;
                }
            }

            // Evita UnassignedReferenceException; igualmente hay que asignar los waypoints.
            if (_currentWaypoint == null)
            {
                _hunter.SetVelocity(Vector3.zero);
                return;
            }

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

            if (_currentWaypoint != null)
            {
                Vector3 steering = Steering.Seek(_hunter, _currentWaypoint, _hunter.MaxSpeed);
                _hunter.SetSteering(steering);
            }
            else
            {
                _hunter.SetVelocity(Vector3.zero);
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

        public override void Enter()
        {
            _hunter.SetAction("Atacando");
        }

        public override void Update()
        {
            if (_hunter.Target == null || _hunter.Target.IsRespawning)
            {
                _hunter.SetTarget(null);
                _stateMachine.ChangeState(HunterStateType.Patrol);
                return;
            }

            if (!_hunter.Target.IsAlive)
            {
                _stateMachine.ChangeState(HunterStateType.Gather);
                return;
            }

            if (_hunter.Perception == null ||
                !_hunter.Perception.InRange(_hunter.Target.transform.position))
            {
                _hunter.SetTarget(null);
                _stateMachine.ChangeState(HunterStateType.Patrol);
                return;
            }

            if (!_hunter.CanAttack)
            {
                _stateMachine.ChangeState(HunterStateType.Chase);
                return;
            }

            float distance = Vector3.Distance(
                _hunter.transform.position,
                _hunter.Target.transform.position
            );

            // Si el boid esta retenido por una trampa, se prioriza el ataque melee.
            bool trapped = _hunter.Target.TargetTrap != null &&
                           _hunter.Target.TargetTrap.IsActive &&
                           _hunter.Target.TargetTrap.IsOccupied;

            if (distance <= _hunter.MeleeAttackRadius)
            {
                _hunter.MeleeAttack();

                // La consigna exige abandonar Attack despues de atacar exitosamente.
                _stateMachine.ChangeState(_hunter.Target.IsAlive
                    ? HunterStateType.Chase : HunterStateType.Gather);
                return;
            }

            if (!trapped && distance <= _hunter.RangeAttackRadius)
            {
                _hunter.RangedAttack();

                // Se mantiene el mismo objetivo en Chase durante el cooldown.
                _stateMachine.ChangeState(_hunter.Target.IsAlive
                    ? HunterStateType.Chase : HunterStateType.Gather);
                return;
            }

            Vector3 steering = Steering.Pursuit(_hunter, _hunter.Target, _hunter.MaxSpeed);
            _hunter.SetSteering(steering);
        }
    }

    private class ChaseState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;

        public ChaseState(Hunter hunter, StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _hunter.SetAction("Persiguiendo la misma presa");
        }

        public override void Update()
        {
            if (_hunter.Target == null || _hunter.Target.IsRespawning)
            {
                _hunter.SetTarget(null);
                _stateMachine.ChangeState(HunterStateType.Patrol);
                return;
            }

            if (!_hunter.Target.IsAlive)
            {
                _stateMachine.ChangeState(HunterStateType.Gather);
                return;
            }

            if (_hunter.Perception == null ||
                !_hunter.Perception.InRange(_hunter.Target.transform.position))
            {
                _hunter.SetTarget(null);
                _stateMachine.ChangeState(HunterStateType.Patrol);
                return;
            }

            if (_hunter.CanAttack)
            {
                _stateMachine.ChangeState(HunterStateType.Attack);
                return;
            }

            float distance = Vector3.Distance(
                _hunter.transform.position,
                _hunter.Target.transform.position
            );

            if (distance <= _hunter.MeleeAttackRadius * 0.75f)
            {
                _hunter.SetVelocity(Vector3.zero);
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
            _hunter.SetAction("Yendo a recolectar");
        }

        public override void Update()
        {
            // Solo se recolectan boids eliminados, nunca presas vivas.
            if (_hunter.Target == null || _hunter.Target.IsAlive || _hunter.Target.IsRespawning)
            {
                _hunter.SetTarget(null);
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
                _hunter.SetAction("Recolectando boid...");
                _hunter.Target.ShowGatherFeedback();
                _gatherTimer += Time.deltaTime;

                if (_gatherTimer >= _gatherDuration)
                {
                    Trap trap = _hunter.Target.TargetTrap;

                    _hunter.Target.StartRespawn();
                    _hunter.RemoveTrap(trap);
                    _hunter.ResetTrapTimer();
                    _hunter.Target.ClearTargetTrap();
                    _hunter.SetTarget(null);
                    _stateMachine.ChangeState(HunterStateType.Patrol);
                }

                return;
            }

            _gatherTimer = 0f;
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
    Chase,
}
