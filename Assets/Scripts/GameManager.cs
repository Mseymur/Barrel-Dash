using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameActive = false;
    
    [Header("UI")]
    public GameObject waitingForUserCanvas;
    public TextMeshProUGUI statusText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private bool isWarmupComplete = false;

    void Start()
    {
        // Freeze game initially
        isGameActive = false;
        isWarmupComplete = false;
        Time.timeScale = 1f; 
        
        if (waitingForUserCanvas) waitingForUserCanvas.SetActive(true);
        if (statusText) statusText.text = "Initializing System...";
        
        // Ensure gestures are ON while waiting
        SetGesturesActive(true);

        // Start Warmup to allow KinectManager to reset
        StartCoroutine(WarmupRoutine());
    }

    System.Collections.IEnumerator WarmupRoutine()
    {
        // Wait distinct time for scene to settle
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[GameManager] Warmup Routine: Attempting to patch KinectManager state...");
        
        KinectManager km = KinectManager.Instance;
        if (km != null)
        {
            // 1. Force Clear Users to reset detection logic
            km.ClearKinectUsers();
            Debug.Log("[GameManager] Forced ClearKinectUsers()");

            // 2. REFRESH AVATAR CONTROLLERS (The Persistent Manager holds old references!)
            // We need to find the ones in THIS scene and give them to the Manager.
            if (km.avatarControllers != null)
            {
                km.avatarControllers.Clear();
                AvatarController[] avatars = FindObjectsOfType<AvatarController>();
                foreach(var av in avatars)
                {
                    km.avatarControllers.Add(av);
                }
                Debug.Log($"[GameManager] Refreshed Avatars: {km.avatarControllers.Count}");
            }
            
            // 3. REFRESH GESTURE LISTENERS
            if (km.gestureListeners != null)
            {
                km.gestureListeners.Clear();
                // Find all MonoBehaviours and check interface
                MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
                foreach(var script in allScripts)
                {
                    if (script is KinectGestures.GestureListenerInterface && script.enabled)
                    {
                        km.gestureListeners.Add(script);
                    }
                }
                Debug.Log($"[GameManager] Refreshed Gesture Listeners: {km.gestureListeners.Count}");
            }
        }

        // Wait a bit more for the Clear to take effect
        yield return new WaitForSeconds(0.5f);
        
        isWarmupComplete = true;
    }

    void Update()
    {
        if (isGameActive) return;

        // Don't check anything until warmup is done
        if (!isWarmupComplete) return;

        KinectManager km = KinectManager.Instance;
        
        if (km == null)
        {
            if (statusText) statusText.text = "Searching for Kinect Manager...";
            return;
        }

        if (!km.IsInitialized())
        {
            if (statusText) statusText.text = "Waiting for Sensor...";
            return;
        }

        // Now that we've waited, this should return FALSE initially on restart,
        // because KinectManager cleared the users.
        if (km.IsUserDetected())
        {
            // User found! Start game.
            StartGame();
        }
        else
        {
            // Update UI to tell user to stand in front
            if (statusText) statusText.text = "Please stand in front of the Kinect...";
        }
    }

    public void StartGame()
    {
        isGameActive = true;
        if (waitingForUserCanvas) waitingForUserCanvas.SetActive(false);
        Debug.Log("[GameManager] User detected! Game Started.");
        
        // Disable Gestures during gameplay
        SetGesturesActive(false);
    }

    public void SetGesturesActive(bool active)
    {
        // BRUTE FORCE DISABLE/ENABLE
        // usage of FindObjectsOfType to catch ALL instances in the scene
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        
        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;
            
            string scriptName = script.GetType().Name;
            
            if (scriptName == "InteractionManager" || scriptName == "InteractionInputModule")
            {
                if (script.enabled != active)
                {
                    script.enabled = active;
                    Debug.Log($"[GameManager] {scriptName} on {script.gameObject.name} set to: {active}");
                }
            }
        }
        
        // Also check specifically for "EventSystem" if it has any other components
        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null)
        {
            // Try to find standard Unity EventSystem input modules if they are interfering?
            // Usually not, but if KinectInputModule inherits from BaseInputModule...
            // Let's stick to the known Kinect scripts first.
        }
    }
}
