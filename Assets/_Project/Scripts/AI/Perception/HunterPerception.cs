using System.Collections.Generic;
using UnityEngine;

public class HunterPerception : MonoBehaviour
{
    // El nombre serializado original se conserva; en la consigna se llama VisionRadius.
    [SerializeField, Min(0.1f)] private float _perceptionRadius = 10f;
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.1f;
    [SerializeField, Min(8)] private int _maxColliders = 128;
    [SerializeField] private LayerMask _detectionLayers = ~0;

    private Collider[] _colliders;
    private readonly List<Boid> _nearbyBoids = new List<Boid>();

    public float PerceptionRadius => _perceptionRadius;
    public float VisionRadius => _perceptionRadius;

    private void Awake()
    {
        _colliders = new Collider[Mathf.Max(8, _maxColliders)];
    }

    private void OnEnable()
    {
        float interval = Mathf.Max(0.05f, _scanInterval);
        Scan();
        InvokeRepeating(nameof(Scan), Random.Range(0.01f, interval), interval);
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
        int count = Physics.OverlapSphereNonAlloc(transform.position, _perceptionRadius, _colliders,
            _detectionLayers, QueryTriggerInteraction.Collide);
        // Si el buffer se llena, crece solo en esa ocasion (no se reserva cada frame).
        while (count == _colliders.Length && _colliders.Length < 4096)
        {
            _colliders = new Collider[_colliders.Length * 2];
            count = Physics.OverlapSphereNonAlloc(transform.position, _perceptionRadius, _colliders,
                _detectionLayers, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < count; i++)
        {
            Collider collider = _colliders[i];
            if (collider == null)
                continue;

            Boid boid = collider.GetComponentInParent<Boid>();
            if (boid != null && boid.isActiveAndEnabled && !boid.IsRespawning && !_nearbyBoids.Contains(boid) &&
                InRange(boid.transform.position))
                _nearbyBoids.Add(boid);
        }
    }

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= _perceptionRadius * _perceptionRadius;
    }

    public Boid DetectBoid()
    {
        Boid closest = null;
        float closestSqr = float.MaxValue;
        foreach (Boid boid in _nearbyBoids)
        {
            if (boid == null || !boid.isActiveAndEnabled || !boid.IsAlive)
                continue;
            float sqr = (boid.transform.position - transform.position).sqrMagnitude;
            if (sqr <= _perceptionRadius * _perceptionRadius && sqr < closestSqr)
            {
                closest = boid;
                closestSqr = sqr;
            }
        }
        return closest;
    }

    public Boid DetectDeadBoid()
    {
        Boid closest = null;
        float closestSqr = float.MaxValue;
        foreach (Boid boid in _nearbyBoids)
        {
            if (boid == null || !boid.isActiveAndEnabled || boid.IsAlive || boid.IsRespawning || boid.RespawnRequested)
                continue;
            float sqr = (boid.transform.position - transform.position).sqrMagnitude;
            if (sqr <= _perceptionRadius * _perceptionRadius && sqr < closestSqr)
            {
                closest = boid;
                closestSqr = sqr;
            }
        }
        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);
    }
}
