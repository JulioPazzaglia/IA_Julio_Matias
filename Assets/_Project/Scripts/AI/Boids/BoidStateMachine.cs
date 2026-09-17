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

        _stateMachine.RegisterState(
            BoidStateType.Evading,
            new EvadingState(_boid, _stateMachine)
        );

        _stateMachine.RegisterState(
            BoidStateType.Hunted,
            new HuntedState(_boid)
        );

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

    private class FlockingState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public FlockingState(
            Boid boid,
            StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Update()
        {
            Hunter hunter = _boid.Perception.DetectHunter();

            if (hunter != null)
            {
                _stateMachine.ChangeState(
                    BoidStateType.Evading
                );

                return;
            }

            _boid.SetSteering(
                _boid.CalculateFlocking()
            );
        }
    }

    private class EvadingState : State
    {
        private Boid _boid;
        private StateMachine _stateMachine;

        public EvadingState(
            Boid boid,
            StateMachine stateMachine)
        {
            _boid = boid;
            _stateMachine = stateMachine;
        }

        public override void Update()
        {
            Hunter hunter = _boid.Perception.DetectHunter();

            if (hunter == null)
            {
                _stateMachine.ChangeState(
                    BoidStateType.Flocking
                );

                return;
            }

            Vector3 steering = Steering.Evade(
                _boid,
                hunter,
                _boid.MaxSpeed
            );

            _boid.SetSteering(steering);
        }
    }

    private class HuntedState : State
    {
        private Boid _boid;

        public HuntedState(Boid boid)
        {
            _boid = boid;
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
    Hunted
}