using UnityEngine;

public class Trap : MonoBehaviour
{
    public bool IsActive { get; private set; } = true;
    public bool IsOccupied { get; private set; }

    public void Activate(Vector3 position)
    {
        transform.position = position;
        IsActive = true;
        IsOccupied = false;
        gameObject.SetActive(true);
    }

    public void Occupy()
    {
        IsOccupied = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsOccupied = false;
        gameObject.SetActive(false);
    }
}