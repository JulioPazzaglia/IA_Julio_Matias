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
    public Hunter DetectHunter()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _perceptionRadius
        );

        foreach (Collider collider in colliders)
        {
            Hunter hunter = collider.GetComponent<Hunter>();

            if (hunter != null)
                return hunter;
        }

        return null;
    }
    public Trap DetectTrap()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _perceptionRadius
        );

        foreach (Collider collider in colliders)
        {
            Trap trap = collider.GetComponent<Trap>();

            if (trap != null && trap.IsActive && !trap.IsOccupied)
                return trap;
        }

        return null;
    }
}