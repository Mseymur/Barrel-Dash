using UnityEngine;
using System.Collections;

/// <summary>
/// Helper script to ensure Kinect is initialized in the Main Menu for hand gesture control.
/// Attach this to any GameObject in the Main Menu scene.
/// </summary>
public class MenuKinectSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Automatically set up Kinect components if they don't exist")]
    public bool autoSetup = true;

    [Header("Component References")]
    [Tooltip("KinectManager reference (will find automatically if not set)")]
    public KinectManager kinectManager;
    
    [Tooltip("InteractionManager reference (will find automatically if not set)")]
    public InteractionManager interactionManager;

    private void Start()
    {
        if (autoSetup)
        {
            StartCoroutine(SetupKinectForMenu());
        }
    }

    private IEnumerator SetupKinectForMenu()
    {
        Debug.Log("[MenuKinectSetup] Setting up Kinect for menu hand gesture control...");

        // 1. Ensure KinectManager exists
        if (kinectManager == null)
        {
            kinectManager = FindObjectOfType<KinectManager>();
        }

        if (kinectManager == null)
        {
            GameObject kmObject = new GameObject("KinectManager");
            kinectManager = kmObject.AddComponent<KinectManager>();
            DontDestroyOnLoad(kmObject); // Prevent destruction on scene load to avoid closing sensor
            Debug.Log("[MenuKinectSetup] Created persistent KinectManager.");
        }
        else
        {
            // Ensure existing manager is also persistent if possible, or at least we found it.
            DontDestroyOnLoad(kinectManager.gameObject);
        }

        // 2. Ensure InteractionManager exists (for hand tracking)
        if (interactionManager == null)
        {
            interactionManager = FindObjectOfType<InteractionManager>();
        }

        if (interactionManager == null)
        {
            GameObject imObject = new GameObject("InteractionManager");
            interactionManager = imObject.AddComponent<InteractionManager>();
            Debug.Log("[MenuKinectSetup] Created InteractionManager.");
        }

        // 3. Ensure InteractionInputModule exists on EventSystem (for UI interaction)
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObject = new GameObject("EventSystem");
            eventSystem = esObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[MenuKinectSetup] Created EventSystem.");
        }

        InteractionInputModule inputModule = eventSystem.GetComponent<InteractionInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InteractionInputModule>();
            Debug.Log("[MenuKinectSetup] Added InteractionInputModule to EventSystem.");
        }

        // 4. Wait for Kinect to initialize
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (kinectManager != null && KinectManager.IsKinectInitialized())
            {
                Debug.Log("[MenuKinectSetup] ✓ Kinect initialized! Hand gestures are now active in the menu.");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!KinectManager.IsKinectInitialized())
        {
            Debug.LogWarning("[MenuKinectSetup] ⚠ Kinect did not initialize within timeout. Hand gestures may not work.");
        }
    }
}

