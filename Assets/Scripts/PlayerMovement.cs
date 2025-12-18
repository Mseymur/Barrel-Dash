using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;

    private CharacterController controller;
    private Animator animator;
    
    private float currentRotationY;
    private float initialRotationY;

    // For auto-walking to a target
    private bool isWalkingToDestination = false;
    private Transform targetDestination;

    void Start()
    {
        // Store initial Y-rotation for clamping
        initialRotationY = NormalizeAngle(transform.eulerAngles.y);
        currentRotationY = initialRotationY;

        // References
        controller = GetComponent<CharacterController>();
        animator   = GetComponentInChildren<Animator>();
    }

    void Update()
    {

        // If we’re currently auto-walking
        if (isWalkingToDestination && targetDestination != null)
        {
            WalkToDestination();
            return;
        }

        // Example of always moving "forward" with vertical=1, but turning with horizontal input
        float vertical   = 1f; 
        float horizontal = Input.GetAxis("Horizontal");

        // Handle rotation clamped around the initial Y
        if (horizontal != 0f)
        {
            float targetRotationY = currentRotationY + horizontal * rotationSpeed * Time.deltaTime;
            // Clamp rotation to ±30° around initial
            currentRotationY = Mathf.Clamp(targetRotationY, initialRotationY - 30f, initialRotationY + 30f);
            transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
        }

        // Move forward
        if (vertical != 0f)
        {
            Vector3 forwardMovement = transform.forward * (vertical * moveSpeed * Time.deltaTime);
            controller.Move(forwardMovement);
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        // Simple gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Public method to start auto-walking to a given destination.
    /// Called by the game logic script if needed.
    /// </summary>
    public void SetDestination(Transform destination)
    {
        targetDestination = destination;
        isWalkingToDestination = true;
    }

    /// <summary>
    /// Moves the character towards targetDestination.
    /// </summary>
    private void WalkToDestination()
    {
        Vector3 direction = (targetDestination.position - transform.position).normalized;
        Vector3 movement  = direction * moveSpeed * Time.deltaTime;
        controller.Move(movement);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        animator.SetBool("isRunning", true);

        // Stop once close enough
        if (Vector3.Distance(transform.position, targetDestination.position) < 0.5f)
        {
            isWalkingToDestination = false;
            animator.SetBool("isRunning", false);
        }
    }

    /// <summary>
    /// Helper to convert angles into a -180 to 180 range.
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}