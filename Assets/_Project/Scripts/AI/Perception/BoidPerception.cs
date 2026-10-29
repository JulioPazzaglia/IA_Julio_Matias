using System.Collections.Generic;
using UnityEngine;

public class BoidPerception : MonoBehaviour
{
    [SerializeField] private float _perceptionRadius = 5f;
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.2f;
    [SerializeField, Min(8)] private int _maxColliders = 64;

    private Collider[] _colliders;
    private readonly List<Boid> _nearbyBoids = new List<Boid>();
    private readonly List<Hunter> _nearbyHunters = new List<Hunter>();
    private readonly List<Trap> _nearbyTraps = new List<Trap>();

    public float PerceptionRadius => _perceptionRadius;
    public IReadOnlyList<Boid> NearbyBoids => _nearbyBoids;

    private void Awake()
    {
        _colliders = new Collider[Mathf.Max(8, _maxColliders)];
    }

    private void OnEnable()
    {
        // Una sola consulta física cada cierto intervalo, no varias por frame.
        Scan();
        InvokeRepeating(nameof(Scan), Random.Range(0.05f, _scanInterval), _scanInterval);
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

        int count = Physics.OverlapSphereNonAlloc(transform.position, _perceptionRadius, _colliders);

        for (int i = 0; i < count; i++)
        {
            Collider collider = _colliders[i];
            if (collider == null)
                continue;

            Boid boid = collider.GetComponentInParent<Boid>();
            if (boid != null && boid.gameObject.activeInHierarchy &&
                boid.transform != transform && !_nearbyBoids.Contains(boid) &&
                InRange(boid.transform.position))
            {
                _nearbyBoids.Add(boid);
            }

            Hunter hunter = collider.GetComponentInParent<Hunter>();
            if (hunter != null && !_nearbyHunters.Contains(hunter) &&
                InRange(hunter.transform.position))
            {
                _nearbyHunters.Add(hunter);
            }

            Trap trap = collider.GetComponentInParent<Trap>();
            if (trap != null && !_nearbyTraps.Contains(trap) &&
                InRange(trap.transform.position))
            {
                _nearbyTraps.Add(trap);
            }
        }
    }

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude
            <= _perceptionRadius * _perceptionRadius;
    }

    public Hunter DetectHunter()
    {
        foreach (Hunter hunter in _nearbyHunters)
        {
            if (hunter != null && hunter.isActiveAndEnabled && InRange(hunter.transform.position))
                return hunter;
        }

        return null;
    }

    public Trap DetectTrap()
    {
        foreach (Trap trap in _nearbyTraps)
        {
            if (trap != null && trap.IsActive && !trap.IsOccupied &&
                InRange(trap.transform.position))
                return trap;
        }

        return null;
    }
}
