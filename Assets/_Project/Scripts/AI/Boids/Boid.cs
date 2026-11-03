using UnityEngine;

public class Boid : Agent
{
    private MeshRenderer _meshRenderer;
    private Color _originalColor;
    private Collider[] _colliders;

    [SerializeField]
    private float _maxSpeed = 5f;

    [SerializeField]
    private float _maxSteering = 10f;

    [SerializeField]
    private float _separationRadius = 2f;

    [SerializeField]
    private BoidPerception _perception;

    [Header("Flocking Weights")]
    [SerializeField] private float _separationWeight = 9f;
    [SerializeField] private float _alignmentWeight = 15f;
    [SerializeField] private float _cohesionWeight = 5f;

    [SerializeField]
    private float _maxLife = 100f;

    [SerializeField]
    private float _respawnDelay = 3f;

    [Header("Interaccion con trampas")]
    [SerializeField] private float _trapDamage = 20f;
    [SerializeField] private float _trapAttackInterval = 1f;

    private float _life;
    private bool _isRespawning;
    public bool IsAlive => _life > 0f && !_isRespawning;
    public bool IsRespawning => _isRespawning;

    private BoidStateMachine _stateMachine;
    private Trap _targetTrap;

    public float MaxSpeed => _maxSpeed;
    public BoidPerception Perception => _perception;
    public Trap TargetTrap => _targetTrap;
    public float TrapDamage => _trapDamage;
    public float TrapAttackInterval => _trapAttackInterval;

    private void Awake()
    {
        if (_perception == null)
            _perception = GetComponent<BoidPerception>();

        _stateMachine = new BoidStateMachine(this);

        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;

        _colliders = GetComponentsInChildren<Collider>();
        _life = _maxLife;

        _velocity = randomDirection.normalized * _maxSpeed;
    }

    private void Update()
    {
        if (_isRespawning)
            return;

        _stateMachine.Update();

        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;

        if (Bounds.Instance != null)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;
    }

    private void ApplySteering(Vector3 steering)
    {
        steering = Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);

        _velocity += steering;

        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
    }

    private Vector3 Separation()
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        // Utiliza los vecinos detectados por BoidPerception, sin consultar Physics.
        foreach (Boid agent in _perception.NearbyBoids)
        {
            if (agent == null || agent == this || !agent.IsAlive)
                continue;

            Vector3 difference = transform.position - agent.transform.position;
            if (difference.sqrMagnitude <= _separationRadius * _separationRadius &&
                difference.sqrMagnitude > 0.0001f)
            {
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
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Boid agent in _perception.NearbyBoids)
        {
            if (agent == null || agent == this || !agent.IsAlive)
                continue;

            if (_perception.InRange(agent.transform.position))
            {
                desired += agent.Velocity;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return desired.normalized * _maxSpeed - _velocity;
    }

    private Vector3 Cohesion()
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (Boid agent in _perception.NearbyBoids)
        {
            if (agent == null || agent == this || !agent.IsAlive)
                continue;

            if (_perception.InRange(agent.transform.position))
            {
                center += agent.transform.position;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        center /= count;

        Vector3 direction = center - transform.position;
        Vector3 desired = direction.normalized * _maxSpeed;

        return desired - _velocity;
    }

    public void SetSteering(Vector3 steering)
    {
        ApplySteering(steering);
    }

    public Vector3 CalculateFlocking()
    {
        return Separation() * _separationWeight
             + Alignment() * _alignmentWeight
             + Cohesion() * _cohesionWeight;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
            return;

        _life = Mathf.Max(0f, _life - damage);
        CancelInvoke(nameof(ResetColor));

        if (_life <= 0f)
        {
            _velocity = Vector3.zero;
            if (_meshRenderer != null)
                _meshRenderer.material.color = Color.gray; // Muerto: gris

            _stateMachine.ChangeState(BoidStateType.Hunted);
            Debug.Log($"{name}: boid eliminado");
        }
        else
        {
            if (_meshRenderer != null)
                _meshRenderer.material.color = Color.red; // Daño: rojo
            Invoke(nameof(ResetColor), 0.15f);
        }
    }

    public void ShowGatherFeedback()
    {
        if (!IsAlive && _meshRenderer != null)
            _meshRenderer.material.color = Color.yellow; // Recoleccion: amarillo
    }

    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    public void SetTargetTrap(Trap trap)
    {
        _targetTrap = trap;
    }

    public void ClearTargetTrap()
    {
        _targetTrap = null;
    }

    public void StartRespawn()
    {
        if (_isRespawning)
            return;

        _isRespawning = true;
        _velocity = Vector3.zero;
        CancelInvoke(nameof(ResetColor));

        // Mantener el GameObject activo: si se desactiva, sus corutinas/temporizadores
        // pueden dejar de ejecutarse. Solo ocultamos malla y colisiones.
        if (_meshRenderer != null)
            _meshRenderer.enabled = false;

        foreach (Collider collider in _colliders)
        {
            if (collider != null)
                collider.enabled = false;
        }

        Debug.Log($"{name}: recolectado, esperando respawn");
        Invoke(nameof(Respawn), _respawnDelay);
    }

    private void Respawn()
    {
        _life = _maxLife;
        _targetTrap = null;

        if (Bounds.Instance != null)
            transform.position = Bounds.Instance.RandomPosition();

        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = randomDirection.normalized * _maxSpeed;

        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = true;
            _meshRenderer.material.color = _originalColor;
        }

        foreach (Collider collider in _colliders)
        {
            if (collider != null)
                collider.enabled = true;
        }

        _isRespawning = false;
        _stateMachine.ChangeState(BoidStateType.Flocking);
    }

    private void ResetColor()
    {
        if (IsAlive && _meshRenderer != null)
            _meshRenderer.material.color = _originalColor;
    }
}
