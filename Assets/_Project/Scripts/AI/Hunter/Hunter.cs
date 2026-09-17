using UnityEngine;

public class Hunter : Agent
{
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;

    [SerializeField] private Transform _waypointA;
    [SerializeField] private Transform _waypointB;

    [SerializeField] private HunterPerception _perception;

    [SerializeField] private float _TBA = 3f;
    [SerializeField] private float _rangeAttackRadius = 8f;
    [SerializeField] private float _meleeAttackRadius = 2f;

    private float _tbaTimer;

    private HunterStateMachine _stateMachine;

    private Boid _target;

    public float MaxSpeed => _maxSpeed;
    public Transform WaypointA => _waypointA;
    public Transform WaypointB => _waypointB;
    public HunterPerception Perception => _perception;

    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;

    public Boid Target => _target;

    public bool CanAttack => _tbaTimer >= _TBA;

    private void Awake()
    {
        _tbaTimer = _TBA;

        _stateMachine = new HunterStateMachine(this);
    }

    private void Update()
    {
        _tbaTimer += Time.deltaTime;

        _stateMachine.Update();

        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;
    }

    public void SetTarget(Boid target)
    {
        _target = target;
    }

    public void SetSteering(Vector3 steering)
    {
        steering = Vector3.ClampMagnitude(
            steering,
            _maxSteering * Time.deltaTime
        );

        _velocity += steering;

        _velocity = Vector3.ClampMagnitude(
            _velocity,
            _maxSpeed
        );
    }

    public void MeleeAttack()
    {
        _velocity = Vector3.zero;
        ResetTBA();

        // Daño melee al Boid
    }

    public void RangedAttack()
    {
        _velocity = Vector3.zero;
        ResetTBA();

        // Daño ranged al Boid
    }

    private void ResetTBA()
    {
        _tbaTimer = 0f;
    }
}