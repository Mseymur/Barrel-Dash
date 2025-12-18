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

    // We store bodies locally to process data, but we get the data from the Manager
    private Body[] bodies;
    
    private bool gameStarted = false;
    private bool isWalkingToDestination = false;
    private Transform targetDestination;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Start the connection routine
        StartCoroutine(StartWithManager());
    }

    private IEnumerator StartWithManager()
    {
        Debug.Log("[KinectPlayerMovement] Waiting for KinectSensorManager...");

        // Ensure Manager exists
        if (KinectSensorManager.Instance == null)
        {
            Debug.LogError("No KinectSensorManager found! Make sure it is in the scene.");
            yield break;
        }

        // Wait for the persistent sensor to be ready
        yield return KinectSensorManager.Instance.WaitForReady();

        // Wait for Sensor to be explicitly OPEN and AVAILABLE
        while (KinectSensorManager.Instance.Sensor == null || 
               !KinectSensorManager.Instance.Sensor.IsOpen || 
               !KinectSensorManager.Instance.Sensor.IsAvailable)
        {
            yield return null;
        }

        // Initialize local body array based on the sensor specs
        if (KinectSensorManager.Instance.Sensor != null)
        {
            bodies = new Body[KinectSensorManager.Instance.Sensor.BodyFrameSource.BodyCount];
        }
        
        gameStarted = true;
        Debug.Log("[KinectPlayerMovement] Linked to Manager. Control Active.");
    }

    void Update()
    {
        if (!gameStarted) return;

        // Priority: Auto-walking (Cutscenes/End game)
        if (isWalkingToDestination && targetDestination != null)
        {
            WalkToDestination();
            return;
        }

        // Standard Kinect Control
        ProcessKinectInput();
    }

    private void ProcessKinectInput()
    {
        // Get the reader from the Manager (do not create a new one)
        var reader = KinectSensorManager.Instance.BodyFrameReader;
        if (reader == null) return;

        bool isTracked = false;

        // Acquire the latest frame
        using (var frame = reader.AcquireLatestFrame())
        {
            if (frame != null)
            {
                if (bodies == null) bodies = new Body[KinectSensorManager.Instance.Sensor.BodyFrameSource.BodyCount];
                
                frame.GetAndRefreshBodyData(bodies);

                // Find the first tracked body
                foreach (var body in bodies)
                {
                    if (body != null && body.IsTracked)
                    {
                        isTracked = true;
                        ApplyRotation(body);
                        MoveForward();
                        break; // Only listen to one player
                    }
                }
            }
        }

        // Apply gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }

        // Update animation state based on tracking
        if (!isTracked)
        {
            animator.SetBool("isRunning", false);
        }
    }

    private void MoveForward()
    {
        Vector3 forwardMovement = transform.forward * (moveSpeed * Time.deltaTime);
        if (controller.enabled)
        {
            controller.Move(forwardMovement);
        }
        animator.SetBool("isRunning", true);
    }

    private void ApplyRotation(Body body)
    {
        // Get Spine rotation
        JointOrientation spineOrientation = body.JointOrientations[JointType.SpineMid];
        Quaternion kinectRotation = new Quaternion(
            spineOrientation.Orientation.X,
            -spineOrientation.Orientation.Y,
            spineOrientation.Orientation.Z,
            spineOrientation.Orientation.W
        );

        // Apply offset (90 degrees Y is common for Kinect->Unity alignment)
        Quaternion rotationOffset = Quaternion.Euler(0, 90, 0); 
        Quaternion adjustedRotation = kinectRotation * rotationOffset;

        // Smoothly rotate
        transform.rotation = Quaternion.Slerp(transform.rotation, adjustedRotation, Time.deltaTime * rotationSpeed);
    }

    // --- Cleanup Fix ---
    // We removed OnDestroy and OnApplicationQuit from here.
    // The KinectSensorManager handles all cleanup now.
    void OnDestroy()
    {
        // Just clear references
        bodies = null;
    }

    // --- Auto Walk Logic ---
    private void WalkToDestination()
    {
        Vector3 direction = (targetDestination.position - transform.position).normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        controller.Move(movement);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        animator.SetBool("isRunning", true);

        if (Vector3.Distance(transform.position, targetDestination.position) < 0.5f)
        {
            isWalkingToDestination = false;
            animator.SetBool("isRunning", false);
        }
    }

    public void SetDestination(Transform destination)
    {
        targetDestination = destination;
        isWalkingToDestination = true;
    }
}