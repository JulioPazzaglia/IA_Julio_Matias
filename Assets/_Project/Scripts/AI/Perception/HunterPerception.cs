using System.Collections.Generic;
using UnityEngine;

public class HunterPerception : MonoBehaviour
{
    [SerializeField]
    private float _perceptionRadius = 10f;

    [SerializeField, Min(0.05f)] private float _scanInterval = 0.2f;
    [SerializeField, Min(8)] private int _maxColliders = 64;

    private Collider[] _colliders;
    private readonly List<Boid> _nearbyBoids = new List<Boid>();

    public float PerceptionRadius => _perceptionRadius;

    private void Awake()
    {
        _colliders = new Collider[Mathf.Max(8, _maxColliders)];
    }

    private void OnEnable()
    {
        Scan();
        InvokeRepeating(nameof(Scan), Random.Range(0.05f, _scanInterval), _scanInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Scan));
        _nearbyBoids.Clear();
    }

    private void Scan()
    {
        if (_colliders == null)
            _colliders = new Collider[Mathf.Max(8, _maxColliders)];

        _nearbyBoids.Clear();

        int count = Physics.OverlapSphereNonAlloc(transform.position, _perceptionRadius, _colliders);

        for (int i = 0; i < count; i++)
        {
            Collider collider = _colliders[i];
            if (collider == null)
                continue;

            Boid boid = collider.GetComponentInParent<Boid>();
            if (boid != null && boid.gameObject.activeInHierarchy &&
                !_nearbyBoids.Contains(boid) && InRange(boid.transform.position))
            {
                _nearbyBoids.Add(boid);
            }
        }
    }

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude
            <= _perceptionRadius * _perceptionRadius;
    }

    public Boid DetectBoid()
    {
        Boid closest = null;
        float closestDistance = float.MaxValue;

        foreach (Boid boid in _nearbyBoids)
        {
            if (boid == null || !boid.IsAlive)
                continue;

            float distance = (boid.transform.position - transform.position).sqrMagnitude;
            if (distance <= _perceptionRadius * _perceptionRadius && distance < closestDistance)
            {
                closest = boid;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public Boid DetectDeadBoid()
    {
        Boid closest = null;
        float closestDistance = float.MaxValue;

        foreach (Boid boid in _nearbyBoids)
        {
            if (boid == null || boid.IsAlive || boid.IsRespawning)
                continue;

            float distance = (boid.transform.position - transform.position).sqrMagnitude;
            if (distance <= _perceptionRadius * _perceptionRadius && distance < closestDistance)
            {
                closest = boid;
                closestDistance = distance;
            }
        }

        return closest;
    }
}
