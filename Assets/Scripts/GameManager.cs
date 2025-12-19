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

    void Start()
    {
        // Freeze game initially
        isGameActive = false;
        Time.timeScale = 1f; // Keep time running for Kinect updates, but we pause logic manually
        
        if (waitingForUserCanvas) waitingForUserCanvas.SetActive(true);
        if (statusText) statusText.text = "Initializing Kinect...";
        
        // Ensure gestures are ON while waiting (or for menu)
        SetGesturesActive(true);
    }

    void Update()
    {
        if (isGameActive) return;

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

        if (km.IsUserDetected())
        {
            // User found! Start game.
            StartGame();
        }
        else
        {
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
