using UnityEngine;

public class BoidStateMachine
{
    private StateMachine _stateMachine;
    private Boid _boid;

    public string CurrentStateName
    {
        get
        {
            if (_stateMachine.CurrentState is FlockingState) return "Flocking";
            if (_stateMachine.CurrentState is EvadeState) return "Evade";
            if (_stateMachine.CurrentState is SeekObjectState) return "Seek Object";
            if (_stateMachine.CurrentState is ConsumeState) return "Consume";
            if (_stateMachine.CurrentState is DeadState) return "Dead";
            if (_stateMachine.CurrentState is RespawnState) return "Respawn";
            return "Sin estado";
        }
    }

    public BoidStateMachine(Boid boid)
    {
        _boid = boid;
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(BoidStateType.Flocking, new FlockingState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.Evade, new EvadeState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.SeekObject, new SeekObjectState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.Consume, new ConsumeState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.Dead, new DeadState(_boid, _stateMachine));
        _stateMachine.RegisterState(BoidStateType.Respawn, new RespawnState(_boid, _stateMachine));
        _stateMachine.ChangeState(BoidStateType.Flocking);
    }

    public void Update()
    {
        // Prioridades del recuperatorio: Dead > Evade > Consume > Seek Object > Flocking.
        // Las transiciones se evalúan en la propia FSM.
        if (!_boid.IsAlive && !_boid.IsRespawning &&
            !(_stateMachine.CurrentState is DeadState) &&
            !(_stateMachine.CurrentState is RespawnState))
        {
            _stateMachine.ChangeState(BoidStateType.Dead);
        }
        else if (_boid.IsAlive && _boid.Perception != null &&
                 _boid.Perception.DetectHunter() != null &&
                 !(_stateMachine.CurrentState is EvadeState))
        {
            _stateMachine.ChangeState(BoidStateType.Evade);
        }
        _stateMachine.Update();
    }

    // Estados en este mismo archivo: se respeta la estructura original del proyecto.
    private class FlockingState : State
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

    private class EvadeState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public EvadeState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _boid.ClearTargetTrap();
            _boid.SetStateColor(Color.cyan);
        }

        public override void Update()
        {
            Hunter hunter = _boid.Perception != null ? _boid.Perception.DetectHunter() : null;
            if (hunter == null)
            {
                _stateMachine.ChangeState(BoidStateType.Flocking);
                return;
            }

            // Evade usa prediccion (Steering.Evade), con Separation como apoyo.
            Vector3 steering = Steering.Evade(_boid, hunter, _boid.MaxSpeed)
                             + _boid.CalculateEvadeSeparation();
            _boid.SetSteering(steering);
        }
    }

    private class SeekObjectState : State
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
            if (trap == null || !trap.isActiveAndEnabled || !trap.IsActive || trap.IsOccupied ||
                (_boid.Perception != null && !_boid.Perception.InVisionRange(trap.transform.position)))
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
            float speed = _boid.MaxSpeed * Mathf.Clamp01((distance - stopDistance) / _boid.ObjectSlowingDistance);
            Vector3 steering = Steering.Arrive(_boid, trap.transform, speed, stopDistance);
            _boid.SetSteering(steering + _boid.CalculateEvadeSeparation());
        }
    }

    private class ConsumeState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;
        private float _consumeTimer;

        public ConsumeState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _consumeTimer = 0f;
            _boid.SetVelocity(Vector3.zero);
            _boid.SetStateColor(Color.magenta);
        }

        public override void Update()
        {
            _boid.SetVelocity(Vector3.zero);
            Trap trap = _boid.TargetTrap;
            if (trap == null || !trap.isActiveAndEnabled || !trap.IsActive)
            {
                _boid.ClearTargetTrap();
                _stateMachine.ChangeState(BoidStateType.Flocking);
                return;
            }

            _consumeTimer += Time.deltaTime;
            if (_consumeTimer >= Mathf.Max(0.1f, _boid.TrapAttackInterval))
            {
                _consumeTimer = 0f;
                trap.TakeDamage(_boid.TrapDamage);
                if (!trap.IsActive)
                {
                    _boid.ClearTargetTrap();
                    _stateMachine.ChangeState(BoidStateType.Flocking);
                }
            }
        }
    }

    private class DeadState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public DeadState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _boid.SetVelocity(Vector3.zero);
            _boid.ClearTargetTrap();
            _boid.SetStateColor(Color.gray);
        }

        public override void Update()
        {
            _boid.SetVelocity(Vector3.zero);
            if (_boid.RespawnRequested)
                _stateMachine.ChangeState(BoidStateType.Respawn);
        }
    }

    private class RespawnState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;
        private float _timer;

        public RespawnState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _timer = 0f;
            _boid.BeginRespawn();
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _boid.RespawnDelay)
            {
                if (_boid.CompleteRespawn())
                    _stateMachine.ChangeState(BoidStateType.Flocking);
                else
                    _timer = _boid.RespawnDelay - _boid.RespawnRetryInterval;
            }
        }
    }
}

public enum BoidStateType
{
    Flocking,
    Evade,
    SeekObject,
    Consume,
    Dead,
    Respawn
}
