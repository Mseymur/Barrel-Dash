using System.Collections;
using UnityEngine;
using Windows.Kinect;

[RequireComponent(typeof(CharacterController))]
public class KinectPlayerController : MonoBehaviour
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

        // Initialize from persistent manager
        StartCoroutine(InitFromManager());
    }

    private IEnumerator InitFromManager()
    {
        Debug.Log("[KinectPlayerController] Waiting for KinectSensorManager...");

        // Disable standard PlayerController to prevent keyboard movement/death while waiting
        PlayerController standardPlayerController = GetComponent<PlayerController>();
        if (standardPlayerController != null)
        {
            standardPlayerController.enabled = false;
            Debug.Log("[KinectPlayerController] Disabled standard PlayerController while waiting for Kinect.");
        }

        // Disable PlayerMovement to prevent auto-run while waiting
        PlayerMovement standardPlayerMovement = GetComponent<PlayerMovement>();
        if (standardPlayerMovement != null)
        {
            standardPlayerMovement.enabled = false;
            Debug.Log("[KinectPlayerController] Disabled standard PlayerMovement while waiting for Kinect.");
        }

        // Ensure Manager exists
        if (KinectSensorManager.Instance == null)
        {
            Debug.LogError("No KinectSensorManager found! Make sure it is in the scene.");
            yield break;
        }

        // 1. Wait for Manager to be initialized
        yield return KinectSensorManager.Instance.WaitForReady();

        // Get references from the manager (shared resources)
        sensor = KinectSensorManager.Instance.Sensor;
        bodyFrameReader = KinectSensorManager.Instance.BodyFrameReader;
        bodies = KinectSensorManager.Instance.Bodies;

        // 2. Hard Wait (4 seconds) to let sensor power cycle/settle
        float initialWait = 4f;
        while (initialWait > 0)
        {
            statusMessage = $"Initializing Kinect... {Mathf.Ceil(initialWait)}";
            initialWait -= Time.deltaTime;
            yield return null;
        }

        // 3. Wait for ACTUAL BODY TRACKING
        Debug.Log("[KinectPlayerController] Waiting for Body Tracking...");
        statusMessage = "Please stand in front of Kinect...";
        
        while (true)
        {
            if (DetectKinectMovement())
            {
                Debug.Log("[KinectPlayerController] Body Detected! Starting Game.");
                statusMessage = "Player Detected! GO!";
                break;
            }
            yield return null;
        }

        // Short pause to show "GO!"
        yield return new WaitForSeconds(1f);

        Debug.Log("[KinectPlayerController] Game Starting.");

        // Re-enable standard PlayerController if needed
        if (standardPlayerController != null)
        {
            standardPlayerController.enabled = true;
        }

        // Re-enable standard PlayerMovement if needed
        if (standardPlayerMovement != null)
        {
            standardPlayerMovement.enabled = true;
        }

        gameStarted = true;
        isWaitingForMovement = false;
    }

    // Removed StartCountdown and WaitForMovementOrTimeout as they are replaced by the logic above

    private bool DetectKinectMovement()
    {
        if (sensor == null || !sensor.IsOpen || !sensor.IsAvailable)
            return false;

        if (bodyFrameReader == null)
            return false;

        using (var frame = bodyFrameReader.AcquireLatestFrame())
        {
            if (frame == null) return false;

            if (bodies == null)
            {
                bodies = new Body[sensor.BodyFrameSource.BodyCount];
            }

            frame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body != null && body.IsTracked)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void Update()
    {
        if (!gameStarted || isWaitingForMovement) return;

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

    void OnDestroy()
    {
        // PROPER FIX: Do NOT close the sensor or dispose the reader.
        // The KinectSensorManager owns these resources.
        // We just clear our local references.
        
        sensor = null;
        bodyFrameReader = null;
        bodies = null;
        
        Debug.Log("[KinectPlayerController] Destroyed. References cleared. Sensor kept open by Manager.");
    }
    
    // OnApplicationQuit is handled by KinectSensorManager
    
    private string statusMessage = "";

    void OnGUI()
    {
        if (!gameStarted)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 40;
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.MiddleCenter;
            
            string msg = "Initializing Kinect...";
            if (KinectSensorManager.Instance != null && KinectSensorManager.Instance.Sensor != null)
            {
                if (!KinectSensorManager.Instance.Sensor.IsAvailable)
                    msg = "Waiting for Kinect Sensor... (Please Wait)";
                else
                    msg = "Kinect Ready! Starting...";
            }
            
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), msg, style);
        }
    }
}