using UnityEngine;

public class Boid : Agent
{
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField] private BoidPerception _perception;

    private BoidStateMachine _stateMachine;

    public float MaxSpeed => _maxSpeed;
    public BoidPerception Perception => _perception;

    private void Awake()
    {
        _stateMachine = new BoidStateMachine(this);
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _velocity = randomDirection.normalized * _maxSpeed;
    }

    private void Update()
    {
        _stateMachine.Update();

        transform.position += _velocity * Time.deltaTime;

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
                desired += transform.position - agent.transform.position;
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
        return Separation() + Alignment() + Cohesion();
    }
}