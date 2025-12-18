using System.Collections;
using UnityEngine;
using Windows.Kinect;

[RequireComponent(typeof(CharacterController))]
public class KinectPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;

    private CharacterController controller;
    private Animator animator;

    // Use the singleton KinectManager instead of managing Kinect directly
    private BodyFrameReader bodyFrameReader;
    private Body[] bodies;
    private bool kinectReady = false;

    // Destination walking
    private bool isWalkingToDestination = false;
    private Transform targetDestination;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator  = GetComponentInChildren<Animator>();

        // Wait for Kinect to be ready using the singleton manager
        StartCoroutine(WaitForKinectAndInitialize());
    }

    private IEnumerator WaitForKinectAndInitialize()
    {
        // Ensure KinectManager exists and is initializing
        KinectManager kinectManager = KinectManager.Instance;
        
        // Wait for Kinect to be ready
        yield return StartCoroutine(kinectManager.WaitForReady());
        
        // Get references from the manager
        bodyFrameReader = kinectManager.BodyFrameReader;
        bodies = kinectManager.Bodies;
        kinectReady = true;
        
        Debug.Log("[KinectPlayerMovement] Kinect is ready, player can now move!");
    }

    void Update()
    {
        // Don't process movement until Kinect is ready
        if (!kinectReady) return;

        // If auto-walking (to a specific target) is happening
        if (isWalkingToDestination && targetDestination != null)
        {
            WalkToDestination();
            return;
        }

        // Normal forward movement
        Vector3 forwardMovement = transform.forward * (moveSpeed * Time.deltaTime);
        if (controller.enabled)
        {
            controller.Move(forwardMovement);
        }

        // Apply rotation from Kinect
        Quaternion userRotation = GetKinectRotation();
        if (userRotation != Quaternion.identity)
        {
            // Example offset to correct alignment (tweak as needed)
            Quaternion rotationOffset = Quaternion.Euler(0, 90, 0);
            Quaternion adjustedRotation = userRotation * rotationOffset;

            // Smoothly interpolate to the new rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                adjustedRotation,
                Time.deltaTime * rotationSpeed
            );
        }

        // Gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }

        // Update animator
        if (controller.velocity.magnitude > 0.1f)
            animator.SetBool("isRunning", true);
        else
            animator.SetBool("isRunning", false);
    }

    /// <summary>
    /// Grabs the rotation from the Kinect spine orientation.
    /// </summary>
    private Quaternion GetKinectRotation()
    {
        if (bodyFrameReader == null || !kinectReady) return Quaternion.identity;

        using (var frame = bodyFrameReader.AcquireLatestFrame())
        {
            if (frame == null) return Quaternion.identity;

            if (bodies == null)
            {
                KinectManager kinectManager = KinectManager.Instance;
                if (kinectManager != null && kinectManager.Sensor != null)
                {
                    bodies = new Body[kinectManager.Sensor.BodyFrameSource.BodyCount];
                }
                else
                {
                    return Quaternion.identity;
                }
            }

            frame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body == null || !body.IsTracked) continue;

                JointOrientation spineOrientation = body.JointOrientations[JointType.SpineMid];
                Quaternion kinectRotation = new Quaternion(
                    spineOrientation.Orientation.X,
                    -spineOrientation.Orientation.Y,
                    spineOrientation.Orientation.Z,
                    spineOrientation.Orientation.W
                );
                return kinectRotation;
            }
        }
        return Quaternion.identity;
    }

    /// <summary>
    /// Auto-walks the character toward targetDestination if triggered.
    /// </summary>
    private void WalkToDestination()
    {
        Vector3 direction = (targetDestination.position - transform.position).normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        controller.Move(movement);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        animator.SetBool("isRunning", true);

        // Arrived near destination
        if (Vector3.Distance(transform.position, targetDestination.position) < 0.5f)
        {
            isWalkingToDestination = false;
            animator.SetBool("isRunning", false);
        }
    }

    /// <summary>
    /// Called by other script when we need to walk automatically to a specific point.
    /// </summary>
    public void SetDestination(Transform destination)
    {
        targetDestination = destination;
        isWalkingToDestination = true;
    }

    void OnDestroy()
    {
        // Don't dispose bodyFrameReader or close sensor here - the KinectManager handles that
        // We just clear our references
        bodyFrameReader = null;
        bodies = null;
        kinectReady = false;
    }
}