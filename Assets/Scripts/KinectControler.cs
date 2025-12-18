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

    // Kinect references
    private KinectSensor sensor;
    private BodyFrameReader bodyFrameReader;
    private Body[] bodies;

    // Destination walking
    private bool isWalkingToDestination = false;
    private Transform targetDestination;

    void Start()
    {
        // Initialize Kinect
        sensor = KinectSensor.GetDefault();
        if (sensor != null)
        {
            // Open the sensor as soon as possible. IsAvailable becomes true *after* Open(),
            // so do NOT gate opening on IsAvailable being true.
            if (!sensor.IsOpen)
            {
                sensor.Open();
            }

            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            Debug.Log("[KinectPlayerMovement] Kinect sensor opened and BodyFrameReader created.");
        }
        else
        {
            Debug.LogWarning("[KinectPlayerMovement] KinectSensor.GetDefault() returned null. Kinect not connected?");
        }

        controller = GetComponent<CharacterController>();
        animator  = GetComponentInChildren<Animator>();
    }

    void Update()
    {

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
        if (bodyFrameReader == null) return Quaternion.identity;

        using (var frame = bodyFrameReader.AcquireLatestFrame())
        {
            if (frame == null) return Quaternion.identity;

            if (bodies == null)
            {
                bodies = new Body[sensor.BodyFrameSource.BodyCount];
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
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }

        // Optionally keep the sensor open between scene loads.
        // If you prefer the Kinect to stay warm across scenes, comment out the block below.
        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
            sensor = null;
            Debug.Log("[KinectPlayerMovement] Kinect sensor closed on destroy.");
        }
    }
}