using System;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] private float _maxLife = 100f;
    private float _life;

    public bool IsActive { get; private set; } = true;
    public bool IsOccupied { get; private set; }

    // Permite que Hunter lleve la cuenta de trampas activas si se destruyen.
    public event Action<Trap> OnDeactivated;

    private void Awake()
    {
        _life = _maxLife;
    }

    public void Activate(Vector3 position)
    {
        transform.position = position;
        _life = _maxLife;
        IsActive = true;
        IsOccupied = false;
        gameObject.SetActive(true);
    }

    public void Occupy()
    {
        if (IsActive)
            IsOccupied = true;
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
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        IsOccupied = false;
        OnDeactivated?.Invoke(this);
        gameObject.SetActive(false);
    }
}
