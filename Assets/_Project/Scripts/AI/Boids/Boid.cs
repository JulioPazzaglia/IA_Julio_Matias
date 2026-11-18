using UnityEngine;

public class Boid : Agent
{
    private MeshRenderer _meshRenderer;
    private Renderer[] _renderers;
    private bool[] _rendererWasEnabled;
    private bool[] _colliderWasEnabled;
    private Collider[] _spawnResults = new Collider[128];
    private Color _originalColor;
    private Collider[] _colliders;

    [SerializeField, Min(0.1f)]
    private float _maxSpeed = 5f;

    [SerializeField, Min(0.1f)]
    private float _maxSteering = 10f;

    [SerializeField, Min(0.1f)]
    private float _separationRadius = 2f;

    [SerializeField]
    private BoidPerception _perception;

    [SerializeField, Min(0.1f)]
    private float _evadeSpeed = 8f;

    public float EvadeSpeed => _evadeSpeed;

    [Header("Flocking Weights")]
    [SerializeField, Min(0f)]
    private float _separationWeight = 9f;

    [SerializeField, Min(0f)]
    private float _alignmentWeight = 15f;

    [SerializeField, Min(0f)]
    private float _cohesionWeight = 5f;

    [SerializeField, Min(0f)]
    private float _evadeSeparationWeight = 0.3f;

    private const int MaxLife = 2;
    private int _life;

    [SerializeField, Min(0.1f)]
    private float _respawnDelay = 3f;

    [SerializeField, Min(0.1f)]
    private float _trapDamage = 20f;

    [SerializeField, Min(0.1f)]
    private float _trapAttackInterval = 1f;

    [Header("Colisiones y feedback")]
    [SerializeField, Min(0.1f)]
    private float _collisionRadius = 0.45f;

    [SerializeField, Min(0.1f)]
    private float _objectSlowingDistance = 3f;

    [SerializeField, Min(0.01f)]
    private float _objectReachTolerance = 0.15f;

    [SerializeField, Min(0.05f)]
    private float _respawnRetryInterval = 0.2f;

    [SerializeField, Min(0.01f)]
    private float _damageFlashTime = 0.2f;

    [SerializeField, Min(0.1f)]
    private float _clearanceScanRadius = 3f;

    [SerializeField]
    private bool _showStateLabel = true;

    private bool _isRespawning;
    private bool _respawnRequested;
    private bool _leftBoundsSinceTargeted;
    private BoidStateMachine _stateMachine;
    private Trap _targetTrap;

    public bool IsAlive => _life > 0 && !_isRespawning;
    public bool IsRespawning => _isRespawning;
    public bool RespawnRequested => _respawnRequested;
    public bool LeftBoundsSinceTargeted => _leftBoundsSinceTargeted;
    public int Life => _life;
    public float MaxSpeed => _maxSpeed;
    public float CollisionRadius => Mathf.Max(0.1f, _collisionRadius);
    public float SeparationRadius =>
        _perception != null
            ? Mathf.Min(_separationRadius, Mathf.Max(0.01f, _perception.PerceptionRadius - 0.01f))
            : _separationRadius;
    public float ObjectSlowingDistance => _objectSlowingDistance;
    public float ObjectReachTolerance => _objectReachTolerance;
    public float RespawnRetryInterval => _respawnRetryInterval;
    public BoidPerception Perception => _perception;
    public Trap TargetTrap => _targetTrap;
    public float TrapDamage => _trapDamage;
    public float TrapAttackInterval => _trapAttackInterval;
    public string CurrentStateName =>
        _stateMachine != null ? _stateMachine.CurrentStateName : "Inicializando";

    private void Awake()
    {
        if (_perception == null)
            _perception = GetComponent<BoidPerception>();

        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;

        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliderWasEnabled = new bool[_colliders.Length];
        _rendererWasEnabled = new bool[_renderers.Length];
        _life = MaxLife;
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = randomDirection.normalized * _maxSpeed;
        _stateMachine = new BoidStateMachine(this);

        if (_perception == null)
            Debug.LogError("Boid: falta BoidPerception.", this);
    }

    private void Update()
    {
        _stateMachine.Update();
        if (!IsAlive)
        {
            _velocity = Vector3.zero;
            return;
        }

        _velocity.y = 0f;
        Vector3 newPosition = transform.position;
        if (_velocity.sqrMagnitude > 0.0001f)
            newPosition = PreventOverlaps(newPosition + _velocity * Time.deltaTime);

        if (Bounds.Instance != null)
        {
            Vector3 wrappedPosition = Bounds.Instance.OutOfBounds(newPosition);
            if ((wrappedPosition - newPosition).sqrMagnitude > 0.0001f)
            {
                bool crossedXZ =
                    Mathf.Abs(wrappedPosition.x - newPosition.x) > 0.001f
                    || Mathf.Abs(wrappedPosition.z - newPosition.z) > 0.001f;
                if (!crossedXZ || IsClearPosition(wrappedPosition))
                {
                    if (crossedXZ)
                        _leftBoundsSinceTargeted = true;
                    newPosition = wrappedPosition;
                }
                else
                {
                    newPosition = transform.position;
                    _velocity = Vector3.zero;
                }
            }
        }

        transform.position = newPosition;
        if (_velocity.sqrMagnitude > 0.0001f)
            transform.forward = _velocity;
    }

    private Vector3 PreventOverlaps(Vector3 position)
    {
        if (_perception == null)
            return position;

        for (int iteration = 0; iteration < 4; iteration++)
        {
            foreach (Boid other in _perception.NearbyBoids)
            {
                if (
                    other == null
                    || other == this
                    || !other.isActiveAndEnabled
                    || other.IsRespawning
                )
                    continue;
                position = AvoidObstacle(
                    position,
                    other.transform.position,
                    CollisionRadius + other.CollisionRadius + 0.05f
                );
            }

            foreach (Trap trap in _perception.NearbyTraps)
            {
                if (trap == null || !trap.isActiveAndEnabled || !trap.IsActive)
                    continue;
                position = AvoidObstacle(
                    position,
                    trap.transform.position,
                    CollisionRadius + trap.CollisionRadius + 0.05f
                );
            }
        }
        return position;
    }

    private Vector3 AvoidObstacle(Vector3 position, Vector3 obstacle, float minDistance)
    {
        Vector3 difference = position - obstacle;
        difference.y = 0f;
        Vector3 start = transform.position - obstacle;
        start.y = 0f;
        Vector3 step = position - transform.position;
        step.y = 0f;
        float a = step.sqrMagnitude;
        float c = start.sqrMagnitude - minDistance * minDistance;
        if (a > 0.000001f && c >= 0f)
        {
            float b = Vector3.Dot(start, step);
            float discriminant = b * b - a * c;
            if (b < 0f && discriminant >= 0f)
            {
                float t = (-b - Mathf.Sqrt(discriminant)) / a;
                if (t >= 0f && t < 1f)
                    return transform.position + step * Mathf.Max(0f, t - 0.001f);
            }
        }
        float distance = difference.magnitude;
        if (distance >= minDistance)
            return position;
        Vector3 away =
            distance > 0.00001f
                ? difference / distance
                : ((GetInstanceID() % 2 == 0) ? Vector3.right : Vector3.forward);
        return position + away * (minDistance - distance);
    }

    private void ApplySteering(Vector3 steering)
    {
        steering.y = 0f;
        steering = Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
        _velocity += steering;
        float speed = CurrentStateName == "Evade" ? _evadeSpeed : _maxSpeed;
        _velocity = Vector3.ClampMagnitude(_velocity, speed);
    }

    private Vector3 Separation()
    {
        if (_perception == null)
            return Vector3.zero;

        Vector3 desired = Vector3.zero;
        int count = 0;
        float radius = SeparationRadius;
        foreach (Boid agent in _perception.NearbyBoids)
        {
            if (agent == null || agent == this || !agent.isActiveAndEnabled || agent.IsRespawning)
                continue;
            Vector3 difference = transform.position - agent.transform.position;
            difference.y = 0f;
            if (difference.sqrMagnitude <= radius * radius)
            {
                if (difference.sqrMagnitude < 0.0001f)
                    difference =
                        GetInstanceID() < agent.GetInstanceID()
                            ? Vector3.right * 0.01f
                            : Vector3.left * 0.01f;
                desired += difference.normalized / Mathf.Max(difference.magnitude, 0.01f);
                count++;
            }
        }
        if (count == 0)
            return Vector3.zero;
        desired /= count;
        return desired.normalized * _maxSpeed - _velocity;
    }

    private Vector3 Alignment()
    {
        if (_perception == null)
            return Vector3.zero;
        Vector3 desired = Vector3.zero;
        int count = 0;
        foreach (Boid agent in _perception.NearbyBoids)
        {
            if (
                agent == null
                || agent == this
                || !agent.isActiveAndEnabled
                || !agent.IsAlive
                || !_perception.InRange(agent.transform.position)
            )
                continue;
            desired += agent.Velocity;
            count++;
        }
        if (count == 0)
            return Vector3.zero;
        desired /= count;
        return desired.normalized * _maxSpeed - _velocity;
    }

    private Vector3 Cohesion()
    {
        if (_perception == null)
            return Vector3.zero;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Boid agent in _perception.NearbyBoids)
        {
            if (
                agent == null
                || agent == this
                || !agent.isActiveAndEnabled
                || !agent.IsAlive
                || !_perception.InRange(agent.transform.position)
            )
                continue;
            center += agent.transform.position;
            count++;
        }
        if (count == 0)
            return Vector3.zero;
        center /= count;
        Vector3 direction = center - transform.position;
        direction.y = 0f;
        return direction.normalized * _maxSpeed - _velocity;
    }

    public void SetSteering(Vector3 steering)
    {
        ApplySteering(steering);
    }

    public Vector3 CalculateFlocking()
    {
        Vector3 steering =
            Separation() * _separationWeight
            + Alignment() * _alignmentWeight
            + Cohesion() * _cohesionWeight;
        if (steering.sqrMagnitude < 0.0001f && _velocity.sqrMagnitude < 0.0001f)
            steering = transform.forward * _maxSpeed;
        return steering;
    }

    public Vector3 CalculateEvadeSeparation()
    {
        return Separation() * _evadeSeparationWeight;
    }

    public void TakeDamage(float damage)
    {
        if (damage > 0f)
            TakeHits(1);
    }

    public void TakeHits(int hits)
    {
        if (!IsAlive || hits <= 0)
            return;

        _life = Mathf.Max(0, _life - hits);
        CancelInvoke(nameof(ResetColor));
        if (_life <= 0)
        {
            _velocity = Vector3.zero;
            SetStateColor(Color.gray);
            Debug.Log($"{name}: muerto (2 golpes acumulados)");
        }
        else
        {
            SetStateColor(Color.red);
            Invoke(nameof(ResetColor), _damageFlashTime);
        }
    }

    public void ShowGatherFeedback()
    {
        if (!IsAlive)
            SetStateColor(Color.yellow);
    }

    public void SetStateColor(Color color)
    {
        if (_meshRenderer != null)
            _meshRenderer.material.color = color;
    }

    public void SetDefaultColor()
    {
        SetStateColor(_originalColor);
    }

    private void ResetColor()
    {
        if (!IsAlive)
            return;
        if (CurrentStateName == "Evade")
            SetStateColor(Color.cyan);
        else if (CurrentStateName == "Seek Object")
            SetStateColor(Color.yellow);
        else if (CurrentStateName == "Consume")
            SetStateColor(Color.magenta);
        else
            SetDefaultColor();
    }

    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    public void SetTargetTrap(Trap trap)
    {
        if (_targetTrap != trap)
            ClearTargetTrap();
        _targetTrap = trap;
    }

    public void ClearTargetTrap()
    {
        if (_targetTrap != null)
            _targetTrap.Release(this);
        _targetTrap = null;
    }

    public float GetTrapStopDistance(Trap trap)
    {
        return CollisionRadius + (trap != null ? trap.CollisionRadius : 0f) + 0.1f;
    }

    public void ClearOutOfBoundsFlag()
    {
        _leftBoundsSinceTargeted = false;
    }

    public void StartRespawn()
    {
        if (!IsAlive && !_isRespawning)
            _respawnRequested = true;
    }

    public void BeginRespawn()
    {
        _respawnRequested = false;
        _isRespawning = true;
        _velocity = Vector3.zero;
        ClearTargetTrap();
        CancelInvoke(nameof(ResetColor));
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;
            _rendererWasEnabled[i] = _renderers[i].enabled;
            _renderers[i].enabled = false;
        }
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == null)
                continue;
            _colliderWasEnabled[i] = _colliders[i].enabled;
            _colliders[i].enabled = false;
        }
        if (_perception != null)
            _perception.enabled = false;
        Debug.Log($"{name}: recolectado, esperando respawn");
    }

    public bool CompleteRespawn()
    {
        Vector3 position;
        if (!FindSafeRespawnPosition(out position))
            return false;
        transform.position = position;
        _life = MaxLife;
        _respawnRequested = false;
        _leftBoundsSinceTargeted = false;
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = direction.normalized * _maxSpeed;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null)
                _renderers[i].enabled = _rendererWasEnabled[i];
        SetDefaultColor();
        for (int i = 0; i < _colliders.Length; i++)
            if (_colliders[i] != null)
                _colliders[i].enabled = _colliderWasEnabled[i];
        _isRespawning = false;
        if (_perception != null)
            _perception.enabled = true;
        return true;
    }

    private bool FindSafeRespawnPosition(out Vector3 position)
    {
        position = transform.position;
        if (Bounds.Instance == null)
            return false;
        for (int attempt = 0; attempt < 25; attempt++)
        {
            Vector3 candidate = Bounds.Instance.RandomPosition();
            if (IsClearPosition(candidate))
            {
                position = candidate;
                return true;
            }
        }
        return false;
    }

    private bool IsClearPosition(Vector3 candidate)
    {
        int count = Physics.OverlapSphereNonAlloc(
            candidate,
            CollisionRadius + _clearanceScanRadius,
            _spawnResults,
            ~0,
            QueryTriggerInteraction.Collide
        );
        if (count >= _spawnResults.Length)
            return false;
        for (int i = 0; i < count; i++)
        {
            Collider col = _spawnResults[i];
            if (col == null)
                continue;
            Boid other = col.GetComponentInParent<Boid>();
            if (
                other != null
                && other != this
                && other.isActiveAndEnabled
                && !other.IsRespawning
                && TooClose(
                    candidate,
                    other.transform.position,
                    CollisionRadius + other.CollisionRadius + 0.1f
                )
            )
                return false;
            Trap trap = col.GetComponentInParent<Trap>();
            if (
                trap != null
                && trap.isActiveAndEnabled
                && trap.IsActive
                && TooClose(
                    candidate,
                    trap.transform.position,
                    CollisionRadius + trap.CollisionRadius + 0.1f
                )
            )
                return false;
        }
        return true;
    }

    private bool TooClose(Vector3 a, Vector3 b, float distance)
    {
        Vector3 delta = a - b;
        delta.y = 0f;
        return delta.sqrMagnitude < distance * distance;
    }

    public float RespawnDelay => _respawnDelay;

    private void OnGUI()
    {
        if (!_showStateLabel || _isRespawning || Camera.main == null)
            return;
        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.3f);
        if (point.z <= 0f)
            return;
        GUI.Label(
            new Rect(point.x - 70f, Screen.height - point.y, 160f, 22f),
            CurrentStateName + " (" + _life + "/2)"
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, SeparationRadius);
    }
}
