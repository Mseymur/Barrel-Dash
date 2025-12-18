using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuController : MonoBehaviour
{  
    
    public AudioClip menuMusic; // Drag your audio clip here in the Inspector
    private AudioSource audioSource;

    [Header("Kinect Settings")]
    [Tooltip("Reference to KinectManager in the scene (optional - will find automatically if not set)")]
    public KinectManager kinectManager;

    void Start()
    {
        // Initialize Kinect for hand gesture control
        InitializeKinectForMenu();

        // Ensure there's an AudioSource component attached to the GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Assign the audio clip to the AudioSource
        audioSource.clip = menuMusic;
        audioSource.loop = true; // Loop the music
        audioSource.playOnAwake = true; // Optional: Play automatically
        audioSource.Play(); // Play the music
    }

    private void InitializeKinectForMenu()
    {
        // Find or create KinectManager
        if (kinectManager == null)
        {
            kinectManager = FindObjectOfType<KinectManager>();
        }

        if (kinectManager == null)
        {
            // Create KinectManager GameObject if it doesn't exist
            GameObject kmObject = new GameObject("KinectManager");
            kinectManager = kmObject.AddComponent<KinectManager>();
            Debug.Log("[MenuController] Created KinectManager for menu control.");
        }

        // Ensure KinectManager is initialized
        if (kinectManager != null)
        {
            StartCoroutine(WaitForKinectInitialization());
        }
        else
        {
            Debug.LogWarning("[MenuController] Could not find or create KinectManager. Hand gestures may not work.");
        }

        // Check for InteractionManager (needed for hand gesture detection)
        InteractionManager interactionManager = FindObjectOfType<InteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogWarning("[MenuController] InteractionManager not found. Hand gestures may not work. Make sure InteractionManager is in the scene.");
        }
        else
        {
            Debug.Log("[MenuController] InteractionManager found. Hand gestures should work.");
        }

        // Check for InteractionInputModule (needed for UI interaction)
        InteractionInputModule inputModule = FindObjectOfType<InteractionInputModule>();
        if (inputModule == null)
        {
            // Try to find EventSystem and add InteractionInputModule
            UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InteractionInputModule>();
                Debug.Log("[MenuController] Added InteractionInputModule to EventSystem for hand gesture UI control.");
            }
            else
            {
                Debug.LogWarning("[MenuController] EventSystem not found. Cannot add InteractionInputModule. Hand gestures may not work.");
            }
        }
        else
        {
            Debug.Log("[MenuController] InteractionInputModule found. UI hand gestures should work.");
        }
    }

    private IEnumerator WaitForKinectInitialization()
    {
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (kinectManager != null && KinectManager.IsKinectInitialized())
            {
                Debug.Log("[MenuController] Kinect initialized! Hand gestures are now active.");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!KinectManager.IsKinectInitialized())
        {
            Debug.LogWarning("[MenuController] Kinect did not initialize within timeout. Hand gestures may not work.");
        }
    }
    
    public void StartGame(string sceneName)
    {
        Debug.Log("Start Game clicked!");
        SceneManager.LoadScene(sceneName); // Replace with your game scene name
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game clicked!");
        Application.Quit(); // Works only in a built game
    }
}