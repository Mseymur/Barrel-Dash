using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerGameLogic : MonoBehaviour
{
    [Header("Collectibles / Score")]
    public TextMeshProUGUI countText;
    public int count = 0;
    public int maxCount = 0;

    [Header("Game State Booleans")]
    public bool isGameOver = false;
    public bool hasWon = false;

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
    public Camera mainCamera;
    public Camera finaleCamera;
    public float cameraTransitionDuration = 2f;

    [Header("Audio Settings")]
    public AudioClip backgroundMusic;
    public AudioClip coinSound;
    public AudioClip deathSound;
    public AudioClip winSound;
    private AudioSource audioSource;

    // Reference to the movement script if we need to trigger auto-walk
    private PlayerController kinectMovement;

    void Start()
    {
        // Setup references
        kinectMovement = GetComponent<PlayerController>();

        // Audio setup
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.clip = backgroundMusic;
        audioSource.Play();

        // Count how many "Money" items exist initially
        maxCount = GameObject.FindGameObjectsWithTag("Money").Length;

        // Hide the Game Over/Win UI at start
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
        if (winCanvas) winCanvas.SetActive(false);

        // Set correct cameras at start
        if (mainCamera) mainCamera.enabled = true;
        if (finaleCamera) finaleCamera.enabled = false;

        UpdateCountText();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Money"))
        {
            // Collect coin
            other.gameObject.SetActive(false);
            count++;
            UpdateCountText();

            // Play coin sound
            PlaySound(coinSound);
        }
        else if (other.gameObject.CompareTag("WinPoint"))
        {
            // Player reached the win point
            HandleWin();
        }
        else if (other.gameObject.CompareTag("NoSpawn"))
        {
            // Example trigger that stops barrel spawner & auto-walk?
            other.gameObject.SetActive(false);
            count++;
            UpdateCountText();

            PlaySound(coinSound);

            if (barrelSpawner != null)
            {
                barrelSpawner.StopSpawning();
            }

            // If you want to do an auto-walk to some destination:
            // kinectMovement.SetDestination( someTransform );
        }
        else if (other.gameObject.CompareTag("Finale"))
        {
            // Finale triggered!
            other.gameObject.SetActive(false);
            count++;
            UpdateCountText();
            PlaySound(coinSound);

            // Rotate character for finale
            transform.rotation = Quaternion.Euler(0, 270, 0);

            // Switch camera to finale camera with an Animator
            if (finaleCamera != null)
            {
                Animator cameraAnimator = finaleCamera.GetComponent<Animator>();
                if (cameraAnimator != null)
                {
                    cameraAnimator.SetTrigger("StartFinale");
                }
                // Enable finale camera, disable main camera
                finaleCamera.enabled = true;
            }
            if (mainCamera != null)
            {
                mainCamera.enabled = false;
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check collisions with hazards
        if (hit.gameObject.CompareTag("Barrel") || hit.gameObject.CompareTag("Spikes"))
        {
            HandleGameOver();
        }
    }

    /// <summary>
    /// Called when the player hits the WinPoint.
    /// </summary>
    public void HandleWin()
    {
        if (hasWon) return;
        hasWon = true;

        // Stop spawning barrels and freeze them
        if (barrelSpawner != null) barrelSpawner.StopSpawning();
        StopAllBarrels();

        // Show Win UI
        if (winCanvas) winCanvas.SetActive(true);
        if (gameOverCanvas) gameOverCanvas.SetActive(false);

        if (winText) winText.text = count.ToString();

        // Stop background music & play win sound
        if (audioSource) audioSource.Stop();
        PlaySound(winSound);
    }

    /// <summary>
    /// Called when the player is hit by a hazard.
    /// </summary>
    public void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Disable movement
        var controller = GetComponent<CharacterController>();
        if (controller) controller.enabled = false;

        // Trigger death anim
        var animator = GetComponentInChildren<Animator>();
        if (animator) animator.SetTrigger("Die");

        // Stop spawner and freeze existing barrels
        if (barrelSpawner != null) barrelSpawner.StopSpawning();
        StopAllBarrels();

        // Show Game Over UI
        if (gameOverCanvas) gameOverCanvas.SetActive(true);
        if (winCanvas) winCanvas.SetActive(false);

        if (gameOverText) gameOverText.text = count.ToString();

        // Stop background music & play death sound
        if (audioSource) audioSource.Stop();
        PlaySound(deathSound);
    }

    /// <summary>
    /// Freeze all rolling barrels in the scene.
    /// </summary>
    private void StopAllBarrels()
    {
        RollBarrel[] barrels = FindObjectsOfType<RollBarrel>();
        foreach (RollBarrel barrel in barrels)
        {
            barrel.StopTorque();
        }
    }

    /// <summary>
    /// Updates the "Money: X / Y" text.
    /// </summary>
    private void UpdateCountText()
    {
        if (countText)
        {
            countText.text = count.ToString();
        }
    }

    /// <summary>
    /// Example of transitioning cameras if you want a smoother approach.
    /// </summary>
    private IEnumerator SwitchToFinaleCamera()
    {
        float timer = 0f;
        while (timer < cameraTransitionDuration)
        {
            timer += Time.deltaTime;
            // Could add smooth blend logic here if desired
            yield return null;
        }

        if (mainCamera) mainCamera.enabled = false;
        if (finaleCamera) finaleCamera.enabled = true;
    }

    /// <summary>
    /// Helper method to play a one-shot sound.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}