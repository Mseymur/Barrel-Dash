using System.Collections;
using UnityEngine;
using TMPro;
// using Windows.Kinect; 

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;
    private CharacterController controller;
    private Animator animator;
    private float currentRotationY;
    private float initialRotationY;

    [Header("Collectibles")]
    public TextMeshProUGUI countText;
    private int count = 0;
    private int maxCount = 0;

    private bool isGameOver = false;
    private bool hasWon = false;
    private bool isWalkingToDestination = false;

    public Transform targetDestination;
    public string triggerTag = "NoSpawn";
    
    [Header("Kinect Controls")]
    public bool useKinect = true;
    public InteractableJointType trackedJoint = InteractableJointType.SpineMid;
    // We map our own enum or just use int to avoid dependency issues if namespace is missing
    public enum InteractableJointType : int { SpineMid = 1, SpineBase = 0, Head = 3 } 
    public float kinectRotationYOffset = -90f; // Adjusted key variable for model alignment
    public bool mirrorUser = true; // Defaulted to true to fix reversed rotation
    private long lockedUserId = 0; // Lock onto the player who starts the game 


    [Header("Spawners")]
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
    public AudioClip backgroundMusic;  // Background music clip
    public AudioClip coinSound;        // Coin collection sound clip
    public AudioClip deathSound;    // Death sound clip
    public AudioClip winSound;      // win sound clip
    private AudioSource audioSource;   // AudioSource component

    void Start()
    {
        initialRotationY = NormalizeAngle(transform.eulerAngles.y);
        currentRotationY = initialRotationY;
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Audio setup
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
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

        // --- GAME FREEZE CHECK ---
        // Default to PAUSED if GameManager is missing or not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameActive)
        {
            animator.SetBool("isRunning", false);
            return;
        }

        if (isWalkingToDestination)
        {
            WalkToDestination();
            return;
        }

        if (useKinect)
        {
            ProcessKinectInput();
        }
        else
        {
            ProcessKeyboardInput();
        }

        // Gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.8f * Time.deltaTime);
        }
    }

    private void ProcessKeyboardInput()
    {
        float vertical = 1f; // Auto run? Or Input.GetAxis("Vertical")? 
        // User original had fixed vertical = 1f; implies auto-run.
        // But let's respect input if they want keyboard control
        // float vertical = Input.GetAxis("Vertical"); 
        
        float horizontal = Input.GetAxis("Horizontal");

        if (horizontal != 0)
        {
            float targetRotationY = currentRotationY + horizontal * rotationSpeed * Time.deltaTime;
            currentRotationY = Mathf.Clamp(targetRotationY, initialRotationY - 30f, initialRotationY + 30f);
            transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }

        if (vertical != 0)
        {
            Vector3 forwardMovement = transform.forward * vertical * moveSpeed * Time.deltaTime;
            controller.Move(forwardMovement);
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }
    }

    private void ProcessKinectInput()
    {
        KinectManager km = KinectManager.Instance;
        if (km == null || !km.IsInitialized())
        {
            animator.SetBool("isRunning", false);
            return;
        }

        // LOCKING LOGIC:
        // If we don't have a locked user, try to find the Primary one.
        if (lockedUserId == 0)
        {
            long potentialId = km.GetPrimaryUserID();
            if (potentialId != 0)
            {
                lockedUserId = potentialId;
                Debug.Log($"[PlayerController] Locked onto UserID: {lockedUserId}");
            }
            else
            {
                // No user found to start with
                animator.SetBool("isRunning", false);
                return;
            }
        }

        // We check if the locked user is still detected.
        // If they are gone, we stop moving (effectively pausing/resetting).
        // Since custom API isn't fully known, we assume if GetJointOrientation returns valid data, we are good.
        // Or we check km.IsUserDetected(lockedUserId) if that exists.
        // Fallback: If GetPrimaryUserID doesn't match and locked user is *lost*, we might be in trouble.
        // But "IsUserDetected" usually checks ANY user. 
        // We will just trust the lockedUserId. If the user leaves, the rotations usually freeze or zero out.

        // MOVEMENT
        Vector3 forwardMove = transform.forward * moveSpeed * Time.deltaTime;
        controller.Move(forwardMove);
        animator.SetBool("isRunning", true);

        // ROTATION
        // Use the locked ID
        Quaternion userRot = km.GetJointOrientation(lockedUserId, (int)trackedJoint, mirrorUser);
        
        // STABILIZATION:
        // User reported unwanted X/Z tilting. We strictly want Y-axis rotation (Yaw).
        // converting to Euler, masking Y, back to Quaternion.
        Vector3 userEuler = userRot.eulerAngles;
        Quaternion flatUserRot = Quaternion.Euler(0, userEuler.y, 0);

        // Use the adjustable offset (Set to -90 based on your scene setup)
        Quaternion rotationOffset = Quaternion.Euler(0, kinectRotationYOffset, 0); 
        
        // Combine: Offset applied to the flattened rotation
        Quaternion targetRotation = flatUserRot * rotationOffset;
        
        // Apply smoothing
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed / 10f);
    }


    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
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

    public void SetDestination(Transform destination)
    {
        targetDestination = destination;
        isWalkingToDestination = true;
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
        lockedUserId = 0; // Reset lock for next game

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
        
        // Enable gestures for UI interaction
        if (GameManager.Instance != null) GameManager.Instance.SetGesturesActive(true);
    }

    public void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        lockedUserId = 0; // Reset lock for next game

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
        
        // Enable gestures for UI interaction
        if (GameManager.Instance != null) GameManager.Instance.SetGesturesActive(true);
    }

    private void StopAllBarrels()
    {
        RollBarrel[] barrels = FindObjectsOfType<RollBarrel>();
        foreach (RollBarrel barrel in barrels)
        {
            barrel.StopTorque();
        }
    }

    private void UpdateCountText()
    {
        countText.text = count.ToString();
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
