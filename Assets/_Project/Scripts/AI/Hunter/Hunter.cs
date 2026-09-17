using UnityEngine;

public enum HunterStateType
{
    Patrol,
    Attack
}

public class Hunter : Agent
{
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;
    [SerializeField] private Transform _waypointA;
    [SerializeField] private Transform _waypointB;
    [SerializeField] private HunterPerception _perception;
    public float MaxSpeed => _maxSpeed;

    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    private StateMachine _stateMachine;
    private HunterPatrolState _patrolState;
    private HunterAttackState _attackState;

    private void Awake()
    {
        _stateMachine = new StateMachine();

        _patrolState = new HunterPatrolState(
    this,
    _waypointA,
    _waypointB,
    _perception,
    _stateMachine
);

        _attackState = new HunterAttackState(
            this,
            _stateMachine
        );

        _stateMachine.RegisterState(HunterStateType.Patrol, _patrolState);
        _stateMachine.RegisterState(HunterStateType.Attack, _attackState);

        _stateMachine.ChangeState(HunterStateType.Patrol);
    }

    private void Update()
    {

        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);

        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;

        _stateMachine.Update();
    }
}
