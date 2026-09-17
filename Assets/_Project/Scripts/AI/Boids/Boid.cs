using UnityEngine;

public class Boid : Agent
{
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;
    [SerializeField] private float _slowingDistance = 3f;
    [SerializeField] private Transform _target;
    [SerializeField] private BoidPerception _perception;
    private void Awake()
    {
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _velocity = randomDirection.normalized * _maxSpeed;
    }
    private void Update()
    {
        //Vector3 steering = Alignment();
        //Vector3 steering = Separation();
        Vector3 steering = Separation() + Alignment() + Cohesion();
        //Vector3 steering = Evade();
        //Vector3 steering = Pursuit();
        //Vector3 steering = Flee();
        //Vector3 steering = Arrive();
        //Vector3 steering = Seek();

        steering = Vector3.ClampMagnitude(
            steering,
            _maxSteering * Time.deltaTime
        );

        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);

        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;
    }

    private Vector3 Seek()
    {
        Vector3 direction = _target.position - transform.position;
        Vector3 desired = direction.normalized * _maxSpeed;

        return desired - _velocity;
    }
    private Vector3 Flee()
    {
        Vector3 direction = _target.position - transform.position;
        Vector3 desired = -direction.normalized * _maxSpeed;

        return desired - _velocity;
    }
    private Vector3 Arrive()
    {
        Vector3 direction = _target.position - transform.position;
        float distance = direction.magnitude;

        float speed = _maxSpeed;

        if (distance < _slowingDistance)
            speed = _maxSpeed * (distance / _slowingDistance);

        Vector3 desired = direction.normalized * speed;

        return desired - _velocity;
    }
    private Vector3 Pursuit()
    {
        Vector3 futurePosition = CalculateFuture();

        Vector3 direction = futurePosition - transform.position;
        Vector3 desired = direction.normalized * _maxSpeed;

        return desired - _velocity;
    }

    private Vector3 Evade()
    {
        Vector3 futurePosition = CalculateFuture();

        Vector3 direction = futurePosition - transform.position;
        Vector3 desired = -direction.normalized * _maxSpeed;

        return desired - _velocity;
    }


    private Vector3 CalculateFuture()
    {
        float distance = Vector3.Distance(
            transform.position,
            _target.position
        );

        float prediction = distance / _maxSpeed;

        return _target.position + _target.GetComponent<Agent>().Velocity * prediction;
    }
    private Vector3 Separation()
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

}