using UnityEngine;

public class HunterPerception : MonoBehaviour
{
    [SerializeField] private float _perceptionRadius = 10f;

    public float PerceptionRadius => _perceptionRadius;

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude
            <= _perceptionRadius * _perceptionRadius;
    }

    public Boid DetectBoid()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _perceptionRadius
        );

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();

            if (boid != null && boid.IsAlive)
                return boid;
        }

        return null;
    }
    public Boid DetectDeadBoid()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            _perceptionRadius
        );

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();

            if (boid != null && !boid.IsAlive)
            {
                Debug.Log(
                    "[HUNTER] Detectó un Boid muerto."
                );

                return boid;
            }
        }

        return null;
    }
}