using UnityEngine;

public class BoidStateMachine
{
    private StateMachine _stateMachine;
    private Boid _boid;

    public BoidStateMachine(Boid boid)
    {
        _boid = boid;

        _stateMachine = new StateMachine();

        _stateMachine.RegisterState(
            BoidStateType.Flocking,
            new FlockingState(_boid, _stateMachine)
        );

        _stateMachine.RegisterState(BoidStateType.Evading, new EvadingState(_boid, _stateMachine));

        _stateMachine.RegisterState(BoidStateType.Lured, new LuredState(_boid, _stateMachine));

        _stateMachine.RegisterState(BoidStateType.Trapped, new TrappedState(_boid, _stateMachine));

        _stateMachine.RegisterState(BoidStateType.Hunted, new HuntedState(_boid));

        _stateMachine.ChangeState(BoidStateType.Flocking);
    }

    public void Update()
    {
        _stateMachine.Update();
    }

    public void ChangeState(BoidStateType state)
    {
        _stateMachine.ChangeState(state);
    }

    // Estados de la FSM (se mantienen en este archivo para respetar la estructura original).

    private class FlockingState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public FlockingState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Update()
        {
            if (_boid.Perception == null)
                return;

            Hunter hunter = _boid.Perception.DetectHunter();

            if (hunter != null)
            {
                _stateMachine.ChangeState(BoidStateType.Evading);
                return;
            }

            Trap trap = _boid.Perception.DetectTrap();

            if (trap != null)
            {
                _boid.SetTargetTrap(trap);
                _stateMachine.ChangeState(BoidStateType.Lured);
                return;
            }

            _boid.SetSteering(_boid.CalculateFlocking());
        }
    }

    private class EvadingState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public EvadingState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _boid.ClearTargetTrap();
        }

        public override void Update()
        {
            if (_boid.Perception == null)
                return;

            Hunter hunter = _boid.Perception.DetectHunter();

            if (hunter == null)
            {
                _stateMachine.ChangeState(BoidStateType.Flocking);
                return;
            }

            Vector3 steering = Steering.Evade(_boid, hunter, _boid.MaxSpeed);
            _boid.SetSteering(steering);
        }
    }

    private class LuredState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public LuredState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Update()
        {
            if (_boid.Perception == null)
                return;

            Hunter hunter = _boid.Perception.DetectHunter();

            if (hunter != null)
            {
                _stateMachine.ChangeState(BoidStateType.Evading);
                return;
            }

            if (
                _boid.TargetTrap == null
                || !_boid.TargetTrap.IsActive
                || _boid.TargetTrap.IsOccupied
            )
            {
                _boid.SetTargetTrap(null);
                _stateMachine.ChangeState(BoidStateType.Flocking);
                return;
            }

            float distance = Vector3.Distance(
                _boid.transform.position,
                _boid.TargetTrap.transform.position
            );

            if (distance <= 1f)
            {
                _boid.SetVelocity(Vector3.zero);
                _boid.TargetTrap.Occupy();
                _stateMachine.ChangeState(BoidStateType.Trapped);
                return;
            }

            Vector3 steering = Steering.Arrive(
                _boid,
                _boid.TargetTrap.transform,
                _boid.MaxSpeed,
                2f
            );

            _boid.SetSteering(steering);
        }
    }

    private class TrappedState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;
        private float _attackTimer;

        public TrappedState(Boid boid, StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _attackTimer = 0f;
        }

        public override void Update()
        {
            _boid.SetVelocity(Vector3.zero);

            if (_boid.TargetTrap == null || !_boid.TargetTrap.IsActive)
            {
                _boid.ClearTargetTrap();
                _stateMachine.ChangeState(BoidStateType.Flocking);
                return;
            }

            _attackTimer += Time.deltaTime;

            if (_attackTimer >= Mathf.Max(0.1f, _boid.TrapAttackInterval))
            {
                _attackTimer = 0f;
                _boid.TargetTrap.TakeDamage(_boid.TrapDamage);

                if (!_boid.TargetTrap.IsActive)
                {
                    _boid.ClearTargetTrap();
                    _stateMachine.ChangeState(BoidStateType.Flocking);
                }
            }
        }
    }

    private class HuntedState : State
    {
        private Boid _boid;

        public HuntedState(Boid boid)
        {
            _boid = boid;
        }

        public override void Enter()
        {
            _boid.SetVelocity(Vector3.zero);
        }

        public override void Update()
        {
            _boid.SetVelocity(Vector3.zero);
        }
    }
}

public enum BoidStateType
{
    Flocking,
    Evading,
    Lured,
    Trapped,
    Hunted,
}
