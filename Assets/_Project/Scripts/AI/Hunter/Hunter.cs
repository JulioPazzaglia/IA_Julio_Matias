using UnityEngine;

public class Hunter : Agent
{
    [SerializeField]
    private float _maxSpeed = 3f;

    [SerializeField]
    private float _maxSteering = 3f;

    [SerializeField]
    private float _meleeDamage = 100f;

    [SerializeField]
    private float _rangeDamage = 50f;

    [SerializeField]
    private Transform _waypointA;

    [SerializeField]
    private Transform _waypointB;

    [SerializeField]
    private Transform _waypointC;

    [SerializeField]
    private Transform _waypointD;

    [SerializeField]
    private HunterPerception _perception;

    [SerializeField]
    private float _TBA = 3f;

    [SerializeField]
    private float _rangeAttackRadius = 8f;

    [SerializeField]
    private float _meleeAttackRadius = 2f;

    [SerializeField]
    private float _attackStopTime = 0.5f;

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

    [SerializeField]
    private Trap _trapPrefab;

    [SerializeField]
    private float _trapInterval = 5f;

    [SerializeField]
    private float _trapVariation = 2f;

    [Header("Feedback")]
    [SerializeField] private bool _showDebugUI = true;
    private string _currentAction = "Patrullando";

    private float _trapTimer;
    private int _activeTrapCount;

    public bool CanAttack => _tbaTimer >= _TBA;

    private void Awake()
    {
        if (_perception == null)
            _perception = GetComponent<HunterPerception>();

        if (_waypointA == null || _waypointB == null || _waypointC == null || _waypointD == null)
            Debug.LogError("Hunter: asigna los cuatro waypoints (A, B, C, D) en el Inspector.", this);

        if (_perception == null)
            Debug.LogError("Hunter: falta HunterPerception en el Inspector.", this);

        if (_trapPrefab == null)
            Debug.LogError("Hunter: falta el prefab Trap en el Inspector.", this);

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

            if (Bounds.Instance != null)
                transform.position = Bounds.Instance.OutOfBounds(transform.position);

            if (_velocity != Vector3.zero)
                transform.forward = _velocity;
        }
    }

    private void OnGUI()
    {
        if (!_showDebugUI || _stateMachine == null)
            return;

        GUI.Box(new Rect(10, 10, 330, 85), "Hunter - IA");
        GUI.Label(new Rect(20, 30, 310, 18), "Estado: " + _stateMachine.CurrentStateName);
        GUI.Label(new Rect(20, 48, 310, 18), "Objetivo: " + (_target != null ? _target.name : "Ninguno"));
        GUI.Label(new Rect(20, 66, 310, 18), "Accion: " + _currentAction);
    }

    public void SetAction(string action)
    {
        _currentAction = action;
    }

    public void SetTarget(Boid target)
    {
        _target = target;
    }

    public void SetSteering(Vector3 steering)
    {
        steering = Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);

        _velocity += steering;

        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
    }

    public void MeleeAttack()
    {
        if (_target == null || !_target.IsAlive || !CanAttack)
            return;

        _velocity = Vector3.zero;
        _target.TakeDamage(_meleeDamage);
        ResetTBA();
        _currentAction = "Ataque cuerpo a cuerpo";
        Debug.Log($"Hunter: ataque cuerpo a cuerpo a {_target.name}");
    }

    public void RangedAttack()
    {
        if (_target == null || !_target.IsAlive || !CanAttack)
            return;

        _velocity = Vector3.zero;
        _target.TakeDamage(_rangeDamage);
        ResetTBA();
        _attackStopTimer = _attackStopTime;
        _currentAction = "Ataque a distancia";
        Debug.Log($"Hunter: disparo a {_target.name}");
    }

    private void ResetTBA()
    {
        _tbaTimer = 0f;
    }

    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    private void GenerateTrap()
    {
        if (_trapPrefab == null)
            return;

        Trap trap = Instantiate(_trapPrefab, transform.position, Quaternion.identity);
        trap.OnDeactivated += OnTrapDeactivated;
        trap.Activate(transform.position);
        _activeTrapCount++;

        _trapTimer = Random.Range(
            Mathf.Max(0.1f, _trapInterval - _trapVariation),
            Mathf.Max(0.1f, _trapInterval + _trapVariation)
        );
    }

    private void OnTrapDeactivated(Trap trap)
    {
        trap.OnDeactivated -= OnTrapDeactivated;
        _activeTrapCount = Mathf.Max(0, _activeTrapCount - 1);
    }

    public void UpdateTrapTimer()
    {
        _trapTimer -= Time.deltaTime;
    }

    public void TryGenerateTrap()
    {
        if (_trapTimer > 0f || _activeTrapCount >= 5 || _trapPrefab == null)
            return;

        GenerateTrap();
    }

    public void RemoveTrap(Trap trap)
    {
        if (trap == null)
            return;

        trap.Deactivate(); // El evento descuenta una unica vez la trampa activa.
    }

    public void ResetTrapTimer()
    {
        _trapTimer = _trapInterval;
    }
}
