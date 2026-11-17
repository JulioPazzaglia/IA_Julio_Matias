using System.Collections.Generic;
using UnityEngine;

public class BoidPerception : MonoBehaviour
{
    // Se conserva el campo original para no perder su valor en los prefabs.
    [SerializeField, Min(0.1f)] private float _perceptionRadius = 5f; // Alignment / Cohesion
    [SerializeField, Min(0.1f)] private float _visionRadius = 6f;     // Cazador / objetos
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.1f;
    [SerializeField, Min(8)] private int _maxColliders = 128;
    [SerializeField] private LayerMask _detectionLayers = ~0;

    private Collider[] _colliders;
    private Boid _self;
    private readonly List<Boid> _nearbyBoids = new List<Boid>();
    private readonly List<Hunter> _nearbyHunters = new List<Hunter>();
    private readonly List<Trap> _nearbyTraps = new List<Trap>();

    public float PerceptionRadius => _perceptionRadius;
    public float VisionRadius => _visionRadius;
    public IReadOnlyList<Boid> NearbyBoids => _nearbyBoids;
    public IReadOnlyList<Trap> NearbyTraps => _nearbyTraps;

    private void Awake()
    {
        _self = GetComponentInParent<Boid>();
        _colliders = new Collider[Mathf.Max(8, _maxColliders)];
    }

    private void OnEnable()
    {
        float interval = Mathf.Max(0.05f, _scanInterval);
        Scan();
        // Una consulta no alloc cada intervalo, NO por cada Update ni por cada fuerza de flocking.
        InvokeRepeating(nameof(Scan), Random.Range(0.01f, interval), interval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Scan));
        _nearbyBoids.Clear();
        _nearbyHunters.Clear();
        _nearbyTraps.Clear();
    }

    private void Scan()
    {
        if (_colliders == null)
            _colliders = new Collider[Mathf.Max(8, _maxColliders)];

        _nearbyBoids.Clear();
        _nearbyHunters.Clear();
        _nearbyTraps.Clear();

        float radius = Mathf.Max(_perceptionRadius, _visionRadius);
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _colliders,
            _detectionLayers, QueryTriggerInteraction.Collide);
        // Si el buffer se llena, crece solo en esa ocasion (no se reserva cada frame).
        while (count == _colliders.Length && _colliders.Length < 4096)
        {
            _colliders = new Collider[_colliders.Length * 2];
            count = Physics.OverlapSphereNonAlloc(transform.position, radius, _colliders,
                _detectionLayers, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < count; i++)
        {
            Collider collider = _colliders[i];
            if (collider == null)
                continue;

            Boid boid = collider.GetComponentInParent<Boid>();
            if (boid != null && boid.isActiveAndEnabled && boid != _self &&
                !boid.IsRespawning && !_nearbyBoids.Contains(boid))
                _nearbyBoids.Add(boid);

            Hunter hunter = collider.GetComponentInParent<Hunter>();
            if (hunter != null && !_nearbyHunters.Contains(hunter) &&
                InVisionRange(hunter.transform.position))
                _nearbyHunters.Add(hunter);

            Trap trap = collider.GetComponentInParent<Trap>();
            if (trap != null && trap.isActiveAndEnabled && trap.IsActive && !_nearbyTraps.Contains(trap))
                _nearbyTraps.Add(trap);
        }
    }

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= _perceptionRadius * _perceptionRadius;
    }

    public bool InVisionRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= _visionRadius * _visionRadius;
    }

    public Hunter DetectHunter()
    {
        foreach (Hunter hunter in _nearbyHunters)
        {
            if (hunter != null && hunter.isActiveAndEnabled && InVisionRange(hunter.transform.position))
                return hunter;
        }
        return null;
    }

    public Trap DetectTrap()
    {
        foreach (Trap trap in _nearbyTraps)
        {
            if (trap != null && trap.IsActive && !trap.IsOccupied )
                return trap;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius); // Alignment/Cohesion
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _visionRadius);
    }
}
