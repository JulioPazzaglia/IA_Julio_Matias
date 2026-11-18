using UnityEngine;

public class RespawnState : State
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