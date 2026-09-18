using UnityEngine;

public class Boid : Agent
{
    private MeshRenderer _meshRenderer;
    private Color _originalColor;

    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _maxSteering = 10f;
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField] private BoidPerception _perception;
    [SerializeField] private float _maxLife = 100f;
    [SerializeField] private float _respawnDelay = 3f;

    private float _life;
    public bool IsAlive => _life > 0f;

    private BoidStateMachine _stateMachine;
    private Trap _targetTrap;

    public float MaxSpeed => _maxSpeed;
    public BoidPerception Perception => _perception;
    public Trap TargetTrap => _targetTrap;

    private void Awake()
    {
        _stateMachine = new BoidStateMachine(this);

        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _meshRenderer = GetComponent<MeshRenderer>();
        _originalColor = _meshRenderer.material.color;

        _life = _maxLife;

        _velocity = randomDirection.normalized * _maxSpeed;
    }

    private void Update()
    {
        _stateMachine.Update();

        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;

        transform.position =
            Bounds.Instance.OutOfBounds(transform.position);

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;
    }

    private void ApplySteering(Vector3 steering)
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

    private Vector3 Separation()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _separationRadius
        );

        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Collider collider in colliders)
        {
            Agent agent = collider.GetComponent<Agent>();

            if (agent == null || agent == this)
                continue;

            if (_perception.InRange(agent.transform.position))
            {
                desired +=
                    transform.position - agent.transform.position;

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
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _perception.PerceptionRadius
        );

        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Collider collider in colliders)
        {
            Agent agent = collider.GetComponent<Agent>();

            if (agent == null || agent == this)
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
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _perception.PerceptionRadius
        );

        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (Collider collider in colliders)
        {
            Agent agent = collider.GetComponent<Agent>();

            if (agent == null || agent == this)
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

        Vector3 direction =
            center - transform.position;

        Vector3 desired =
            direction.normalized * _maxSpeed;

        return desired - _velocity;
    }

    public void SetSteering(Vector3 steering)
    {
        ApplySteering(steering);
    }

    public Vector3 CalculateFlocking()
    {
        return Separation() * 9f
             + Alignment() * 15f
             + Cohesion() * 5f;
    }

    public void TakeDamage(float damage)
    {
        _life -= damage;

        _meshRenderer.material.color = Color.red;

        Invoke(
            nameof(ResetColor),
            0.15f
        );

        Debug.Log(
            "[BOID] " + gameObject.name +
            " recibió " + damage +
            " de daño. Vida: " + _life
        );

        if (_life <= 0f)
        {
            _life = 0f;

            Debug.Log(
                "[BOID] " + gameObject.name +
                " murió."
            );

            _stateMachine.ChangeState(
                BoidStateType.Hunted
            );
        }
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
        gameObject.SetActive(false);

        Invoke(
            nameof(Respawn),
            _respawnDelay
        );
    }

    private void Respawn()
    {
        _life = _maxLife;

        transform.position =
            Bounds.Instance.RandomPosition();

        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _velocity =
            randomDirection.normalized * _maxSpeed;

        gameObject.SetActive(true);

        _stateMachine.ChangeState(
            BoidStateType.Flocking
        );

        Debug.Log(
            "[BOID] " + gameObject.name +
            " reapareció."
        );
    }

    private void ResetColor()
    {
        _meshRenderer.material.color = _originalColor;
    }
}