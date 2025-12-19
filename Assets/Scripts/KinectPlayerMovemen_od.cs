using System.Collections;
using UnityEngine;
using TMPro;
using Windows.Kinect;

public class KinectPlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;
    private CharacterController controller;
    private Animator animator;
    private float currentRotationY; // To track the current Y rotation
    private float initialRotationY; // To store the initial Y rotation

    private KinectSensor sensor;
    private BodyFrameReader bodyFrameReader;
    private Body[] bodies;
    private bool initialized = false;

    [Header("Collectibles")]
    public TextMeshProUGUI countText;
    private int count = 0;
    private int maxCount = 0;

    private bool isGameOver = false;
    private bool hasWon = false;
    private bool isWalkingToDestination = false;

    public Transform targetDestination;
    public string triggerTag = "NoSpawn";

    [Header("Barrel Spawner")]
    public BarrelSpawner barrelSpawner;

    [Header("Win Settings")]
    public Transform winPoint;

    [Header("UI Settings")]
    public GameObject gameOverCanvas;
    public GameObject winCanvas;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI winText;

    [Header("Camera Settings")]
    public Camera mainCamera;             // The camera following the character (drag your Main Camera here)
    public Camera finaleCamera;           // The camera for the finale animation (drag the Finale Camera here)
    public float cameraTransitionDuration = 2f; // Smooth transition duration in seconds

    [Header("Audio Settings")]
    public AudioClip backgroundMusic;  
    public AudioClip coinSound;        
    public AudioClip deathSound;      
    public AudioClip winSound;        
    private UnityEngine.AudioSource audioSource;   // Fixed namespace conflict

    void Start()
    {
        // Initialize Kinect
        sensor = KinectSensor.GetDefault();
        if (sensor != null && sensor.IsAvailable)
        {
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            sensor.Open();
        }

        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Audio setup
        audioSource = GetComponent<UnityEngine.AudioSource>();
        if (audioSource == null)
        {
            audioSource = GetComponent<UnityEngine.AudioSource>();
        }

        // Play background music
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.Play();

        maxCount = GameObject.FindGameObjectsWithTag("Money").Length;
        Debug.Log($"Game Started! Total Money to Collect: {maxCount}");

        // Ensure only the Game UI Canvas is active at the start
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
        if (winCanvas) winCanvas.SetActive(false);

        // Ensure only the main camera is active at the start
        if (mainCamera) mainCamera.enabled = true;
        if (finaleCamera) finaleCamera.enabled = false;

        UpdateCountText();
    }

    void Update()
    {
        if (isGameOver || hasWon) return;

        if (isWalkingToDestination)
        {
            WalkToDestination();
            return;
        }

        Vector3 forwardMovement = transform.forward * moveSpeed * Time.deltaTime;
        if (controller.enabled)
        {
            controller.Move(forwardMovement);
        }

        Quaternion userRotation = GetKinectRotation();

        if (userRotation != Quaternion.identity)
        {
            // Apply an offset to correct the initial rotation
            Quaternion rotationOffset = Quaternion.Euler(0, 90, 0); // Example offset, adjust as needed
            Quaternion adjustedRotation = userRotation * rotationOffset;

            // Smoothly apply the adjusted rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, adjustedRotation, Time.deltaTime * rotationSpeed);
        }

        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }

        if (controller.velocity.magnitude > 0)
        {
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }
    }


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

                // Log the rotation for debugging
                Debug.Log($"Kinect Rotation (Raw): {kinectRotation.eulerAngles}");

                return kinectRotation;
            }
        }

        return Quaternion.identity;
    }


    void OnDestroy()
    {
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

    void WalkToDestination()
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

void OnTriggerEnter(Collider other)
{
    if (other.gameObject.CompareTag("Money"))
    {
        other.gameObject.SetActive(false);
        count++;
        Debug.Log($"Money Collected! Current Count: {count}/{maxCount}");
        UpdateCountText();

        // Play coin sound
        if (audioSource != null && coinSound != null)
        {
            audioSource.PlayOneShot(coinSound);
        }
    }

    if (other.gameObject.CompareTag("WinPoint"))
    {
        Debug.Log("Player reached the win point!");
        HandleWin();
    }

    if (other.gameObject.CompareTag("NoSpawn"))
    {
        other.gameObject.SetActive(false);
        count++;
        // Play coin sound
        if (audioSource != null && coinSound != null)
        {
            audioSource.PlayOneShot(coinSound);
        }
        Debug.Log("Triggered auto-walk!");
        
        UpdateCountText();
        if (barrelSpawner != null)
        {
            barrelSpawner.StopSpawning();
        }
    }

    if (other.gameObject.CompareTag("Finale"))
    {
        Debug.Log("Finale triggered! Switching to animation camera.");
        other.gameObject.SetActive(false);
        count++;
        UpdateCountText();
        // Adjust character rotation
        transform.rotation = Quaternion.Euler(0, 270, 0); // Example: Face backward (180 degrees on the Y-axis)
        Debug.Log("Character rotation adjusted for finale.");
        // Play coin sound
        if (audioSource != null && coinSound != null)
        {
            audioSource.PlayOneShot(coinSound);
        }

        // Trigger the finale animation
        if (finaleCamera != null)
        {
            Animator cameraAnimator = finaleCamera.GetComponent<Animator>();
            if (cameraAnimator != null)
            {
                cameraAnimator.SetTrigger("StartFinale");
                Debug.Log("Finale animation triggered.");
            }

            // Enable the finale camera
            finaleCamera.enabled = true;
        }

        // Deactivate the main camera
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
        }
    }
}

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Barrel") || hit.gameObject.CompareTag("Spikes"))
        {
            Debug.Log("Player hit! Game Over triggered.");
            HandleGameOver();
        }
    }

    public void HandleWin()
    {
        if (hasWon) return;
        hasWon = true;

        Debug.Log("You Win!");

        if (barrelSpawner != null) barrelSpawner.StopSpawning();
        StopAllBarrels();

        animator.SetBool("isRunning", false);

        // Show Win Canvas and hide others
        if (winCanvas) winCanvas.SetActive(true);
        if (gameOverCanvas) gameOverCanvas.SetActive(false);

        winText.text = count.ToString();

        // Stop background music
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        // Play death sound
        if (audioSource != null && winSound != null)
        {
            audioSource.PlayOneShot(winSound);
        }
    }

    private void StopAllBarrels()
    {
        RollBarrel[] barrels = FindObjectsOfType<RollBarrel>();
        foreach (RollBarrel barrel in barrels)
        {
            barrel.StopTorque();
        }
    }

    public void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("Game Over!");

        controller.enabled = false;

        animator.SetTrigger("Die");

        if (barrelSpawner != null) barrelSpawner.StopSpawning();
        StopAllBarrels();

        // Show Game Over Canvas and hide others
        if (gameOverCanvas) gameOverCanvas.SetActive(true);
        if (winCanvas) winCanvas.SetActive(false);

        gameOverText.text = count.ToString();

        // Stop background music
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }

    private void UpdateCountText()
    {
        countText.text = "Money: " + count + " / " + maxCount;
    }
        private IEnumerator SwitchToFinaleCamera()
    {
        Debug.Log("Switching to finale camera...");
        float timer = 0f;

        while (timer < cameraTransitionDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Disable main camera and enable finale camera
        if (mainCamera) mainCamera.enabled = false;
        if (finaleCamera) finaleCamera.enabled = true;

        Debug.Log("Finale camera active.");
    }
}