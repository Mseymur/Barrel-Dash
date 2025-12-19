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
        if (KinectManager.Instance != null)
        {
            // 1. InteractionManager
            DisableComponentByName(KinectManager.Instance.gameObject, "InteractionManager", active);
            
            // 2. InteractionInputModule (often on EventSystem or Manager)
            DisableComponentByName(KinectManager.Instance.gameObject, "InteractionInputModule", active);
            
            // Check EventSystem separately
            GameObject eventSystem = GameObject.Find("EventSystem");
            if (eventSystem != null)
            {
               DisableComponentByName(eventSystem, "InteractionInputModule", active);
            }
        }
    }

    private void DisableComponentByName(GameObject target, string componentName, bool active)
    {
        if (target == null) return;
        
        MonoBehaviour component = target.GetComponent(componentName) as MonoBehaviour;
        if (component != null)
        {
            component.enabled = active;
            Debug.Log($"[GameManager] {componentName} on {target.name} set to: {active}");
        }
        else
        {
             // Fallback search in scene if not found on target (only if we really expect it)
             // But for safety, let's just log if we find it elsewhere?
             // Actually, let's just look for the object globally if not found on target
             if (componentName == "InteractionManager")
             {
                 GameObject obj = GameObject.Find(componentName);
                 if (obj)
                 {
                     component = obj.GetComponent(componentName) as MonoBehaviour;
                     if (component) component.enabled = active;
                 }
             }
        }
    }
}
