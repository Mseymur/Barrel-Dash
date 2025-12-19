using UnityEngine;

public class RollBarrel : MonoBehaviour
{
    public float torque = 10f; // Base rolling torque
    public float rollSpeed = 5f; // Adjustable speed multiplier
    public float collisionSlowdown = 0.5f; // How much speed decreases on collision

    private Rigidbody rb;
    private bool applyTorque = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Freeze logic if Game Manager is waiting for user
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return;
        }

        if (applyTorque)
        {
            // Apply rolling torque as long as applyTorque is true
            rb.AddTorque(Vector3.back * torque * rollSpeed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // When the barrel enters a NoRollZone
        if (other.CompareTag("NoRollZone"))
        {
            Debug.Log($"Barrel {name} entered NoRollZone. Stopping torque naturally.");
            StopTorqueNaturally();
        }
    }

    public void StopTorqueNaturally()
    {
        // Disable torque application
        applyTorque = false;

        // Let the barrel roll down naturally without adding additional torque
        Debug.Log($"Torque stopped naturally for barrel: {name}. Barrel will roll due to momentum.");
    }

    public void StopTorque()
    {
        // Stop applying torque and zero out angular velocity
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero; // Stop angular movement
            rb.AddTorque(Vector3.zero);       // Stop applying torque
        }

        Debug.Log($"Stopped torque completely for barrel: {name}");
    }

    public void StopRolling()
    {
        // Completely stop the barrel's movement
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Disable physics interactions
        }

        Debug.Log($"Rolling completely stopped for barrel: {name}");
    }
}