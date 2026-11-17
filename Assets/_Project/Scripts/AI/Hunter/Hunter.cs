using UnityEngine;

public class Hunter : Agent
{
    [SerializeField, Min(0.1f)] private float _maxSpeed = 3f;
    [SerializeField, Min(0.1f)] private float _maxSteering = 3f;

    // Se conservan las referencias originales a los cuatro waypoints.
    [SerializeField] private Transform _waypointA;
    [SerializeField] private Transform _waypointB;
    [SerializeField] private Transform _waypointC;
    [SerializeField] private Transform _waypointD;
    [SerializeField] private HunterPerception _perception;

    [Header("Ataques")]
    [SerializeField, Min(0.1f)] private float _TBA = 3f;
    [SerializeField, Min(0.1f)] private float _rangeAttackRadius = 8f;
    [SerializeField, Min(0.1f)] private float _meleeAttackRadius = 2f;
    [SerializeField, Min(0f)] private float _attackStopTime = 0.5f;

    [Header("Energia (solo Hunt / Rest)")]
    [SerializeField, Min(1f)] private float _maxEnergy = 20f;
    [SerializeField, Min(0.01f)] private float _energyConsumptionRate = 1f;
    [SerializeField, Min(0.01f)] private float _energyRechargeRate = 4f;
    [SerializeField] private float _energy; // visible en Inspector durante Play

    [Header("Patrulla y recoleccion")]
    [SerializeField, Min(0.1f)] private float _waypointReachDistance = 1f;
    [SerializeField, Min(0.1f)] private float _gatherDistance = 1f;
    [SerializeField, Min(0.1f)] private float _gatherDuration = 2f;
    [SerializeField, Min(0.1f)] private float _lastPositionReachDistance = 0.5f;
    [SerializeField] private Trap _trapPrefab;
    [SerializeField, Min(0.1f)] private float _trapInterval = 5f;
    [SerializeField, Min(0f)] private float _trapVariation = 2f;
    [SerializeField, Min(0.05f)] private float _trapRetryDelay = 0.5f;
    [SerializeField, Min(0.1f)] private float _trapClearanceScanRadius = 3f;
    private readonly Collider[] _trapSpawnResults = new Collider[128];

    [Header("Feedback")]
    [SerializeField] private bool _showDebugUI = true;
    private string _currentAction = "Patrullando";
    private string _lastAttack = "Ninguno";

    private float _attackStopTimer;
    private float _tbaTimer;
    private float _trapTimer;
    private HunterStateMachine _stateMachine;
    private Boid _target;

    public float MaxSpeed => _maxSpeed;
    public Transform WaypointA => _waypointA;
    public Transform WaypointB => _waypointB;
    public Transform WaypointC => _waypointC;
    public Transform WaypointD => _waypointD;
    public float WaypointReachDistance => _waypointReachDistance;
    public float GatherDistance => _gatherDistance;
    public float GatherDuration => _gatherDuration;
    public float LastPositionReachDistance => _lastPositionReachDistance;
    public float VisionRadius => _perception != null ? _perception.VisionRadius : 0f;
    public HunterPerception Perception => _perception;
    public float RangeAttackRadius => Mathf.Max(_rangeAttackRadius, MeleeAttackRadius + 0.01f);
    public float MeleeAttackRadius => Mathf.Max(0.1f, _meleeAttackRadius);
    public float Energy => _energy;
    public float MaxEnergy => _maxEnergy;
    public Boid Target => _target;
    public bool CanAttack => _tbaTimer >= _TBA;

    private void Awake()
    {
        if (_perception == null)
            _perception = GetComponent<HunterPerception>();

        if (_waypointA == null || _waypointB == null || _waypointC == null || _waypointD == null)
            Debug.LogError("Hunter: asigna los cuatro waypoints (A, B, C y D) en el Inspector.", this);
        if (_perception == null)
            Debug.LogError("Hunter: falta HunterPerception.", this);
        if (_trapPrefab == null)
            Debug.LogError("Hunter: falta el prefab Trap.", this);

        _rangeAttackRadius = Mathf.Max(_rangeAttackRadius, _meleeAttackRadius + 0.1f);
        _energy = _maxEnergy;
        _trapTimer = _trapInterval;
        _tbaTimer = _TBA;
        _stateMachine = new HunterStateMachine(this);
    }

    private void Update()
    {
        // TBA avanza en todos los estados, incluso Gather y Rest.
        _tbaTimer += Time.deltaTime;
        _stateMachine.Update();

        // El disparo pausa el desplazamiento, NO la actualizacion de la FSM ni el gasto de energia.
        if (_attackStopTimer > 0f)
        {
            _attackStopTimer -= Time.deltaTime;
            _velocity = Vector3.zero;
        }

        _velocity.y = 0f;
        transform.position += _velocity * Time.deltaTime;
        if (Bounds.Instance != null)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);
        if (_velocity.sqrMagnitude > 0.0001f)
            transform.forward = _velocity;
    }

    private void OnGUI()
    {
        if (!_showDebugUI || _stateMachine == null)
            return;

        GUI.Box(new Rect(10, 10, 360, 156), "Hunter - IA");
        GUI.Label(new Rect(20, 30, 320, 18), "Estado: " + _stateMachine.CurrentStateName);
        GUI.Label(new Rect(20, 48, 320, 18), "Objetivo: " + (_target != null ? _target.name : "Ninguno"));
        GUI.Label(new Rect(20, 66, 320, 18), "Accion: " + _currentAction);
        GUI.Label(new Rect(20, 84, 320, 18), "Energia: " + _energy.ToString("F1") + " / " + _maxEnergy.ToString("F1"));
        GUI.Label(new Rect(20, 102, 340, 18), "TBA restante: " + Mathf.Max(0f, _TBA - _tbaTimer).ToString("F1") + " s");
        GUI.Label(new Rect(20, 120, 340, 18), "Ultimo ataque: " + _lastAttack);
        GUI.Label(new Rect(20, 138, 340, 18), "Objetos activos: " + Trap.ActiveCount + "/5");
    }

    public void SetAction(string action)
    {
        _currentAction = action;
    }

    public void SetTarget(Boid target)
    {
        _target = target;
        if (_target != null && _target.IsAlive)
            _target.ClearOutOfBoundsFlag();
    }

    public void SetSteering(Vector3 steering)
    {
        steering.y = 0f;
        steering = Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
    }

    public void MeleeAttack()
    {
        if (_target == null || !_target.IsAlive || !CanAttack)
            return;
        _velocity = Vector3.zero;
        _target.TakeHits(2); // Melee = 2 golpes; mata al instante.
        ResetTBA();
        _currentAction = "Ataque cuerpo a cuerpo (2 golpes)";
        _lastAttack = "Melee: " + _target.name;
        Debug.Log($"Hunter: melee a {_target.name} (2 golpes)");
    }

    public void RangedAttack()
    {
        if (_target == null || !_target.IsAlive || !CanAttack)
            return;
        _velocity = Vector3.zero;
        _target.TakeHits(1); // Ranged = 1 golpe.
        ResetTBA();
        _attackStopTimer = _attackStopTime;
        _currentAction = "Ataque a distancia (1 golpe)";
        _lastAttack = "Ranged: " + _target.name;
        Debug.Log($"Hunter: ranged a {_target.name} (1 golpe)");
    }

    private void ResetTBA()
    {
        _tbaTimer = 0f;
    }

    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    // Solamente HuntState llama a este metodo.
    public void ConsumeEnergy()
    {
        _energy = Mathf.Max(0f, _energy - _energyConsumptionRate * Time.deltaTime);
    }

    // Solamente RestState llama a este metodo.
    public void RechargeEnergy()
    {
        _energy = Mathf.Min(_maxEnergy, _energy + _energyRechargeRate * Time.deltaTime);
    }

    private void GenerateTrap()
    {
        if (_trapPrefab == null)
            return;
        Trap trap = Instantiate(_trapPrefab, transform.position, Quaternion.identity);
        trap.Activate(transform.position);
        _trapTimer = Random.Range(Mathf.Max(0.1f, _trapInterval - _trapVariation),
                                  Mathf.Max(0.1f, _trapInterval + _trapVariation));
    }

    public void UpdateTrapTimer()
    {
        _trapTimer -= Time.deltaTime;
    }

    public void TryGenerateTrap()
    {
        if (_trapTimer > 0f || Trap.ActiveCount >= 5 || _trapPrefab == null)
            return;
        // No crear un objeto sobre un boid ni sobre otro objeto; reintentar durante Patrol.
        if (!CanPlaceTrap())
        {
            _trapTimer = _trapRetryDelay;
            return;
        }
        GenerateTrap();
    }

    private bool CanPlaceTrap()
    {
        Vector3 position = transform.position;
        float radius = _trapPrefab.CollisionRadius;
        int count = Physics.OverlapSphereNonAlloc(position, radius + _trapClearanceScanRadius,
            _trapSpawnResults, ~0, QueryTriggerInteraction.Collide);
        if (count >= _trapSpawnResults.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Collider col = _trapSpawnResults[i];
            if (col == null) continue;
            Boid boid = col.GetComponentInParent<Boid>();
            if (boid != null && !boid.IsRespawning && Overlaps(position, boid.transform.position, radius + boid.CollisionRadius + 0.1f))
                return false;
            Trap trap = col.GetComponentInParent<Trap>();
            if (trap != null && trap.isActiveAndEnabled && trap.IsActive && Overlaps(position, trap.transform.position, radius + trap.CollisionRadius + 0.1f))
                return false;
        }
        return true;
    }

    private bool Overlaps(Vector3 a, Vector3 b, float distance)
    {
        Vector3 delta = a - b;
        delta.y = 0f;
        return delta.sqrMagnitude < distance * distance;
    }

    public void RemoveTrap(Trap trap)
    {
        if (trap != null)
            trap.Deactivate();
    }

    public void ResetTrapTimer()
    {
        _trapTimer = _trapInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);
    }
}
