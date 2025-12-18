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
    private bool gameStarted = false;
    private bool isWaitingForMovement = true;

    // Destination walking
    private bool isWalkingToDestination = false;
    private Transform targetDestination;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator  = GetComponentInChildren<Animator>();

        // Initialize Kinect
        InitializeKinect();

        // Start waiting for movement detection
        StartCoroutine(WaitForMovementOrTimeout());
    }

    private void InitializeKinect()
    {
        sensor = KinectSensor.GetDefault();
        if (sensor != null)
        {
            // If sensor is already open (from previous scene), reuse it
            if (!sensor.IsOpen)
            {
                sensor.Open();
                Debug.Log("[KinectPlayerMovement] Kinect sensor opened.");
            }
            else
            {
                Debug.Log("[KinectPlayerMovement] Kinect sensor already open (reusing from previous scene).");
            }
            
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            bodies = new Body[sensor.BodyFrameSource.BodyCount];
            Debug.Log("[KinectPlayerMovement] Kinect initialized. Sensor open: " + sensor.IsOpen + ", Available: " + sensor.IsAvailable);
        }
        else
        {
            Debug.LogWarning("[KinectPlayerMovement] KinectSensor.GetDefault() returned null. Kinect not connected?");
        }
    }

    private IEnumerator WaitForMovementOrTimeout()
    {
        float waitTime = 5f; // 5 seconds max wait
        float elapsed = 0f;
        
        Debug.Log("[KinectPlayerMovement] Waiting for Kinect movement detection (max 5 seconds)...");
        
        while (elapsed < waitTime && !gameStarted)
        {
            // Check if Kinect detects any body movement
            if (DetectKinectMovement())
            {
                gameStarted = true;
                isWaitingForMovement = false;
                Debug.Log("[KinectPlayerMovement] Movement detected! Game starting now.");
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Timeout reached, start anyway
        gameStarted = true;
        isWaitingForMovement = false;
        Debug.Log("[KinectPlayerMovement] Wait timeout reached. Starting game.");
    }

    private bool DetectKinectMovement()
    {
        // Check if sensor is ready
        if (sensor == null || !sensor.IsOpen || !sensor.IsAvailable)
            return false;

        if (bodyFrameReader == null)
            return false;

        // Try to get a frame - if null, Kinect might still be initializing
        using (var frame = bodyFrameReader.AcquireLatestFrame())
        {
            if (frame == null) 
            {
                // Frame is null - Kinect might still be warming up after restart
                return false;
            }

            if (bodies == null)
            {
                bodies = new Body[sensor.BodyFrameSource.BodyCount];
            }

            frame.GetAndRefreshBodyData(bodies);

            // Check for any tracked body
            foreach (var body in bodies)
            {
                if (body == null || !body.IsTracked) continue;
                
                // If we detect a tracked body, that means Kinect is working and reading you
                return true;
            }
        }
        return false;
    }

    void Update()
    {
        // Don't process movement until game has started (after wait period)
        if (!gameStarted || isWaitingForMovement) return;

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
        if (bodyFrameReader == null || sensor == null) return Quaternion.identity;

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
        // Don't close the sensor on scene reload - this causes power-cycling!
        // Only dispose the reader (it's recreated on next scene load)
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }

        // Clear references but DON'T close sensor
        sensor = null;
        bodies = null;
    }

    void OnApplicationQuit()
    {
        // Only close sensor when application is actually quitting
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }

        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
            sensor = null;
        }
    }
}