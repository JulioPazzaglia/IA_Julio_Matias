using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float _height = 30f;
    [SerializeField] private float _width = 60f;
    [SerializeField] private bool _drawGizmos;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 newPosition = position;

        if (position.x > _width / 2)
            newPosition.x = -_width / 2;

        if (position.x < -_width / 2)
            newPosition.x = _width / 2;

        if (position.z > _height / 2)
            newPosition.z = -_height / 2;

        if (position.z < -_height / 2)
            newPosition.z = _height / 2;

        if (position.y < 0f)
            newPosition.y = 1f;

        return newPosition;
    }

    public Vector3 RandomPosition()
    {
        float x = Random.Range(
            -_width / 2,
            _width / 2
        );

        float z = Random.Range(
            -_height / 2,
            _height / 2
        );

        return new Vector3(x, 1f, z);
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(_width, 0, _height)
        );
    }
}