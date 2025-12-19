using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class KinectPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;
    
    // Joint ID to track (SpineMid is usually good for center mass)
    private KinectInterop.JointType trackedJoint = KinectInterop.JointType.SpineMid;
    
    // Components
    private CharacterController controller;
    private Animator animator;

    // Destination Walking (for Cutscenes/Auto-walk)
    private bool isWalkingToDestination = false;
    private Transform targetDestination;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 1. Check Game Freeze State
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            // Game is paused waiting for user. 
            // Ensure idle animation
            if (animator) animator.SetBool("isRunning", false);
            return; 
        }

        // 2. Handle Auto-Walk (if active)
        if (isWalkingToDestination && targetDestination != null)
        {
            WalkToDestination();
            return;
        }

        // 3. Standard Kinect Movement
        ProcessKinectMovement();
    }

    private void ProcessKinectMovement()
    {
        // Get the official KinectManager instance
        KinectManager manager = KinectManager.Instance;

        // Verify manager is happy
        if (manager == null || !manager.IsInitialized() || !manager.IsUserDetected())
        {
            if (animator) animator.SetBool("isRunning", false);
            return;
        }

        // Get the Primary User ID
        long userId = manager.GetPrimaryUserID();

        // If we have a user...
        if (userId != 0)
        {
            // --- ROTATION ---
            // Get user's rotation from the Spine
            Quaternion userRot = manager.GetUserOrientation(userId, (int)trackedJoint, false);
            
            // Adjust for alignment (Kinect users face -Z usually, Unity models face +Z)
            // This offset might need tweaking depending on your specific model
            Quaternion rotationOffset = Quaternion.Euler(0, 180, 0); 
            Quaternion targetRotation = userRot * rotationOffset;

            // Apply smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed / 10f);


            // --- MOVEMENT ---
            // We just move forward whenever the user is tracked and the game is active
            // You can add gesture checks here if you want them to "Stop"
            Vector3 forwardMove = transform.forward * moveSpeed * Time.deltaTime;
            controller.Move(forwardMove);

            if (animator) animator.SetBool("isRunning", true);
        }
        else
        {
             if (animator) animator.SetBool("isRunning", false);
        }

        // Gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }
    }

    // --- Auto Walk Logic (Shared with old script) ---
    private void WalkToDestination()
    {
        Vector3 direction = (targetDestination.position - transform.position).normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        controller.Move(movement);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (animator) animator.SetBool("isRunning", true);

        if (Vector3.Distance(transform.position, targetDestination.position) < 0.5f)
        {
            isWalkingToDestination = false;
            if (animator) animator.SetBool("isRunning", false);
        }
    }

    public void SetDestination(Transform destination)
    {
        targetDestination = destination;
        isWalkingToDestination = true;
    }
}