using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float speed = 50f; // Rotation speed around Z-axis

    void Update()
    {
        // Rotate the object around its Z-axis
        transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}