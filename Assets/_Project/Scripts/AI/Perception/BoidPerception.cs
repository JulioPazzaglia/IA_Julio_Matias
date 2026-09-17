using UnityEngine;

public class BoidPerception : MonoBehaviour
{
    [SerializeField] private float _perceptionRadius = 5f;

    public float PerceptionRadius => _perceptionRadius;

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude
            <= _perceptionRadius * _perceptionRadius;
    }
}