using UnityEngine;

public class HunterStateMachine
{
    private StateMachine _stateMachine;
    private Hunter _hunter;

    public string CurrentStateName
    {
        get
        {
            if (_stateMachine.CurrentState is PatrolState) return "Patrol";
            if (_stateMachine.CurrentState is HuntState) return "Hunt";
            if (_stateMachine.CurrentState is GatherState) return "Gather";
            if (_stateMachine.CurrentState is RestState) return "Rest";
            return "Sin estado";
        }
    }

    public HunterStateMachine(Hunter hunter)
    {
        _hunter = hunter;
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(HunterStateType.Patrol, new PatrolState(_hunter, _stateMachine));
        _stateMachine.RegisterState(HunterStateType.Hunt, new HuntState(_hunter, _stateMachine));
        _stateMachine.RegisterState(HunterStateType.Gather, new GatherState(_hunter, _stateMachine));
        _stateMachine.RegisterState(HunterStateType.Rest, new RestState(_hunter, _stateMachine));
        _stateMachine.ChangeState(HunterStateType.Patrol);
    }

    public void Update()
    {
        _stateMachine.Update();
    }

    // Estados anidados: misma estructura que la StateMachine del proyecto original.
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
                if (_currentWaypointIndex == 0) _currentWaypoint = _hunter.WaypointA;
                else if (_currentWaypointIndex == 1) _currentWaypoint = _hunter.WaypointB;
                else if (_currentWaypointIndex == 2) _currentWaypoint = _hunter.WaypointC;
                else _currentWaypoint = _hunter.WaypointD;
            }

            if (_currentWaypoint != null)
                _hunter.SetSteering(Steering.Seek(_hunter, _currentWaypoint, _hunter.MaxSpeed));
            else
                _hunter.SetVelocity(Vector3.zero);
        }
    }

    private class HuntState : State
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
            // La presa se elige EN Hunt, a partir de lo que percibe su sensor.
            Boid candidate = _hunter.Perception != null ? _hunter.Perception.DetectBoid() : null;
            _hunter.SetTarget(candidate);
            if (candidate != null)
                _lastKnownPosition = candidate.transform.position;
        }

        public override void Update()
        {
            // La energia se consume solamente en Hunt.
            _hunter.ConsumeEnergy();
            if (_hunter.Energy <= 0f)
            {
                _stateMachine.ChangeState(HunterStateType.Rest);
                return;
            }

            Boid target = _hunter.Target;
            if (target != null && target.IsAlive && target.LeftBoundsSinceTargeted)
            {
                // El boid puede haber sido devuelto al otro borde por Bounds.OutOfBounds.
                _stateMachine.ChangeState(HunterStateType.Patrol);
                return;
            }

            // Un muerto visible permite abandonar Hunt para recolectarlo.
            Boid deadBoid = _hunter.Perception != null ? _hunter.Perception.DetectDeadBoid() : null;
            if (deadBoid != null)
            {
                _hunter.SetTarget(deadBoid);
                _stateMachine.ChangeState(HunterStateType.Gather);
                return;
            }

            if (target == null || !target.isActiveAndEnabled || !target.IsAlive || target.IsRespawning)
            {
                // Si la presa murio, seleccionar otro vivo QUE EL SENSOR PERCIBE.
                Boid replacement = _hunter.Perception != null ? _hunter.Perception.DetectBoid() : null;
                _hunter.SetTarget(replacement);
                target = replacement;
                if (target == null)
                {
                    MoveToLastKnownPosition();
                    return; // Sin presa: Hunt persiste hasta Rest/Gather.
                }
            }

            // No se consulta el radio de vision para abandonar a una presa viva.
            // Se sigue con su referencia original mientras siga viva.
            _lastKnownPosition = target.transform.position;
            Vector3 delta = target.transform.position - _hunter.transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (_hunter.CanAttack && distance <= _hunter.MeleeAttackRadius)
            {
                _hunter.MeleeAttack();  // 2 golpes
                return;                 // Permanece en Hunt.
            }
            if (_hunter.CanAttack && distance <= _hunter.RangeAttackRadius)
            {
                _hunter.RangedAttack(); // 1 golpe
                return;                 // Permanece en Hunt.
            }

            _hunter.SetAction("Persiguiendo con Pursuit");
            _hunter.SetSteering(Steering.Pursuit(_hunter, target, _hunter.MaxSpeed));
        }

        private void MoveToLastKnownPosition()
        {
            Vector3 difference = _lastKnownPosition - _hunter.transform.position;
            difference.y = 0f;
            if (difference.sqrMagnitude <= _hunter.LastPositionReachDistance * _hunter.LastPositionReachDistance)
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

    private class GatherState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;
        private float _gatherTimer;

        public GatherState(Hunter hunter, StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _gatherTimer = 0f;
            _hunter.SetAction("Buscando cuerpo para recolectar");
        }

        public override void Update()
        {
            Boid target = _hunter.Target;
            if (target == null || !target.isActiveAndEnabled || target.IsAlive || target.IsRespawning || target.RespawnRequested)
            {
                _stateMachine.ChangeState(HunterStateType.Patrol);
                return;
            }

            Vector3 delta = target.transform.position - _hunter.transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance <= _hunter.GatherDistance)
            {
                _hunter.SetVelocity(Vector3.zero);
                _hunter.SetAction("Recolectando boid...");
                target.ShowGatherFeedback();
                _gatherTimer += Time.deltaTime;

                if (_gatherTimer >= _hunter.GatherDuration)
                {
                    target.StartRespawn(); // Solicitud, transicion realizada por la FSM del Boid.
                    _stateMachine.ChangeState(HunterStateType.Patrol);
                }
                return;
            }

            _gatherTimer = 0f;
            _hunter.SetAction("Yendo al cuerpo con Seek");
            _hunter.SetSteering(Steering.Seek(_hunter, target.transform, _hunter.MaxSpeed));
        }
    }

    private class RestState : State
    {
        private Hunter _hunter;
        private StateMachine _stateMachine;

        public RestState(Hunter hunter, StateMachine stateMachine)
        {
            _hunter = hunter;
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            _hunter.SetTarget(null);
            _hunter.SetVelocity(Vector3.zero);
            _hunter.SetAction("Descansando y recargando energia");
        }

        public override void Update()
        {
            // Durante Rest no se detecta, persigue, ataca ni recolecta.
            _hunter.SetVelocity(Vector3.zero);
            _hunter.RechargeEnergy();
            if (_hunter.Energy >= _hunter.MaxEnergy)
                _stateMachine.ChangeState(HunterStateType.Patrol);
        }
    }
}

public enum HunterStateType
{
    Patrol,
    Hunt,
    Gather,
    Rest
}
