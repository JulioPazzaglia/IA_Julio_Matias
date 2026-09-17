using UnityEngine;

public class Hunter : Agent
{
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;
    [SerializeField] private float _meleeDamage = 100f;
    [SerializeField] private float _rangeDamage = 50f;

    [SerializeField] private Transform _waypointA;
    [SerializeField] private Transform _waypointB;
    [SerializeField] private Transform _waypointC;
    [SerializeField] private Transform _waypointD;

    [SerializeField] private HunterPerception _perception;

    [SerializeField] private float _TBA = 3f;
    [SerializeField] private float _rangeAttackRadius = 8f;
    [SerializeField] private float _meleeAttackRadius = 2f;
    [SerializeField] private float _attackStopTime = 0.5f;

    private float _attackStopTimer;

    private float _tbaTimer;

    private HunterStateMachine _stateMachine;

    private Boid _target;

    public float MaxSpeed => _maxSpeed;

    public Transform WaypointA => _waypointA;
    public Transform WaypointB => _waypointB;
    public Transform WaypointC => _waypointC;
    public Transform WaypointD => _waypointD;

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

        if (_attackStopTimer > 0f)
        {
            _attackStopTimer -= Time.deltaTime;
            _velocity = Vector3.zero;
        }
        else
        {
            _stateMachine.Update();

            transform.position += _velocity * Time.deltaTime;

            transform.position =
                Bounds.Instance.OutOfBounds(transform.position);

            if (_velocity != Vector3.zero)
                transform.forward = _velocity;
        }
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

        Debug.Log("[HUNTER] Ataque melee");

        _target.TakeDamage(_meleeDamage);

        ResetTBA();
    }

    public void RangedAttack()
    {
        _velocity = Vector3.zero;

        Debug.Log("[HUNTER] Ataque ranged");

        _target.TakeDamage(_rangeDamage);

        ResetTBA();

        _attackStopTimer = _attackStopTime;
    }

    private void ResetTBA()
    {
        _tbaTimer = 0f;
    }
    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }
}