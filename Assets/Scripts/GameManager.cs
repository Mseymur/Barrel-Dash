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
            // We use string reflection to avoid missing type errors
            MonoBehaviour interactionManager = KinectManager.Instance.GetComponent("InteractionManager") as MonoBehaviour;
            if (interactionManager != null)
            {
                interactionManager.enabled = active;
                Debug.Log($"[GameManager] InteractionManager enabled: {active}");
            }
            else
            {
                // Fallback: Try to find it in the scene if not on the same object
                GameObject interactionObj = GameObject.Find("InteractionManager");
                if (interactionObj == null) interactionObj = GameObject.Find("KinectController");
                
                if (interactionObj != null)
                {
                    interactionManager = interactionObj.GetComponent("InteractionManager") as MonoBehaviour;
                    if (interactionManager != null)
                    {
                        interactionManager.enabled = active;
                        Debug.Log($"[GameManager] InteractionManager (found separately) enabled: {active}");
                    }
                }
            }
        }
    }
}
