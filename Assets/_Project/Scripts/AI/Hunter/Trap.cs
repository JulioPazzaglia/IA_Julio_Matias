using System;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _maxLife = 100f;
    [SerializeField, Min(0.1f)] private float _collisionRadius = 0.6f;
    private float _life;
    private bool _occupied;
    private Boid _occupant;
    private bool _counted;
    public static int ActiveCount { get; private set; }
    public float Life => _life;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCount() { ActiveCount = 0; }

    private void OnEnable() { RegisterActive(); }
    private void OnDisable() { UnregisterActive(); }

    private void RegisterActive()
    {
        if (!_counted && IsActive && isActiveAndEnabled)
        {
            _counted = true;
            ActiveCount++;
        }
    }

    private void UnregisterActive()
    {
        if (_counted)
        {
            _counted = false;
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }
    }

    public bool IsActive { get; private set; } = true;
    public bool IsOccupied => _occupied && (_occupant == null || _occupant.IsAlive);
    public float CollisionRadius => _collisionRadius;

    public event Action<Trap> OnDeactivated;

    private void Awake()
    {
        _life = _maxLife;
    }

    public void Activate(Vector3 position)
    {
        transform.position = position;
        _life = _maxLife;
        _occupied = false;
        _occupant = null;
        IsActive = true;
        gameObject.SetActive(true);
        RegisterActive();
    }

    public void Occupy()
    {
        if (IsActive)
            _occupied = true;
    }

    public bool Occupy(Boid boid)
    {
        if (!IsActive || !isActiveAndEnabled || boid == null || !boid.IsAlive || (IsOccupied && _occupant != boid))
            return false;
        _occupant = boid;
        _occupied = true;
        return true;
    }

    public void Release(Boid boid)
    {
        if (_occupant == boid)
        {
            _occupied = false;
            _occupant = null;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsActive || damage <= 0f)
            return;

        _life = Mathf.Max(0f, _life - damage);
        if (_life <= 0f)
        {
            Debug.Log($"{name}: objeto de interes destruido");
            Deactivate();
            Destroy(gameObject);
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;
        IsActive = false;
        UnregisterActive();
        _occupied = false;
        _occupant = null;
        OnDeactivated?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _collisionRadius);
    }
}
