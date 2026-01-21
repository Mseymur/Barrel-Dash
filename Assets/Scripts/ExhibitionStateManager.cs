using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ExhibitionStateManager : MonoBehaviour
{
    public enum ExhibitionState
    {
        Warmup,
        Idle,           // Waiting for ANY user (Main Menu)
        Scanning,       // User found, locking in (5s)
        Gameplay,       // Game Active, checking for abandonment
        ResultScreen    // Win/Lose, checking 15s timeout or New User
    }

    [Header("Configuration")]
    [Tooltip("Time required to stand in front to become Primary Player")]
    public float lockInDuration = 5.0f;
    [Tooltip("Time before game resets to menu if abandoned or finished")]
    public float abandonTimeout = 15.0f;
    [Tooltip("Show visual progress bar during lock-in?")]
    public bool enableVisualFeedback = true;
    public string mainMenuSceneName = "MainMenu";

    [Header("Tracking Stability")]
    [Tooltip("Ignore users closer than this (Z-depth)")]
    public float minZ = 0.5f;
    [Tooltip("Ignore users further than this (Z-depth)")]
    public float maxZ = 2.5f;
    [Tooltip("Ignore users further than this from center (X-axis abs)")]
    public float boundaryX = 1.5f;
    [Tooltip("Time to wait before resetting if user is momentarily lost (Occlusion)")]
    public float recoveryGracePeriod = 1.0f;

    [Header("References")]
    public LockInFeedback lockInFeedbackPrefab; 
    
    // We observe these to know when to enter "Result" state
    public GameObject winCanvas;
    [Tooltip("The 'Warning' text object inside the Win Canvas")]
    public GameObject winWarningText; 

    public GameObject loseCanvas; // "GO_Canvas"
    [Tooltip("The 'Warning' text object inside the Lose/Game Over Canvas")]
    public GameObject loseWarningText;

    [Header("Debug")]
    public ExhibitionState currentState = ExhibitionState.Warmup;
    public float curStateTimer = 0f;
    public float gameIdleTimer = 0f; 

    private long _pendingUserId = 0;
    private LockInFeedback _feedbackInstance;
    private GameObject _activeWarningText; // Found dynamically
    private float _recoveryTimer = 0f; // For occlusion handling

    void Start()
    {
        // Find or Spawn Feedback UI
        if (lockInFeedbackPrefab != null && _feedbackInstance == null)
        {
             if (lockInFeedbackPrefab.gameObject.scene.name == null)
             {
                _feedbackInstance = Instantiate(lockInFeedbackPrefab);
                DontDestroyOnLoad(_feedbackInstance.gameObject); 
             }
             else
             {
                _feedbackInstance = lockInFeedbackPrefab;
             }
        }
        
        StartCoroutine(WarmupRoutine());
        
        if (winCanvas == null) Debug.LogWarning("ExhibitionManager: Win Canvas is NOT assigned!");
        if (loseCanvas == null) Debug.LogWarning("ExhibitionManager: Lose Canvas is NOT assigned!");
    }

    IEnumerator WarmupRoutine()
    {
        currentState = ExhibitionState.Warmup;
        yield return new WaitForSeconds(2.0f); // Wait for GameManager warmup
        
        // RESTART CHECK:
        // If GameManager preserved the user, we should have a Primary ID already.
        KinectManager km = KinectManager.Instance;
        long existingPrimary = km != null ? km.GetPrimaryUserID() : 0;
        
        if (existingPrimary != 0 && km.IsUserTracked(existingPrimary))
        {
             Debug.Log($"[ExhibitionManager] Found existing Primary User {existingPrimary}. Fast-tracking to Game.");
             currentState = ExhibitionState.Gameplay;
             GameManager.Instance.StartGame();
        }
        else
        {
             currentState = ExhibitionState.Idle;
        }
    }

    void Update()
    {
        if (currentState == ExhibitionState.Warmup) return;

        KinectManager km = KinectManager.Instance;
        if (km == null || !km.IsInitialized()) return;

        // Check for Game Over / Win State externally
        bool isResultActive = (winCanvas != null && winCanvas.activeSelf) || (loseCanvas != null && loseCanvas.activeSelf);
        
        // State Machine Transition to Result
        if (isResultActive && currentState != ExhibitionState.ResultScreen)
        {
            EnterResultState();
        }
        else if (!isResultActive && currentState == ExhibitionState.ResultScreen)
        {
             currentState = ExhibitionState.Gameplay;
        }

        switch (currentState)
        {
            case ExhibitionState.Idle:
                HandleIdle(km);
                break;
            case ExhibitionState.Scanning:
                HandleScanning(km);
                break;
            case ExhibitionState.Gameplay:
                HandleGameplay(km);
                break;
            case ExhibitionState.ResultScreen:
                HandleResultScreen(km);
                break;
        }
    }

    void HandleIdle(KinectManager km)
    {
        // SMART SELECTION:
        // Use the helper to find the BEST user in the zone, not just the first one.
        long bestUser = GetBestUserInZone(km);

        if (bestUser != 0)
        {
            _pendingUserId = bestUser;
            curStateTimer = 0f;
            currentState = ExhibitionState.Scanning;
        }
        else
        {
            // Abandonment Logic for Pre-Game (Wait State)
            if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            {
                 gameIdleTimer += Time.deltaTime;
                 if (gameIdleTimer >= abandonTimeout) 
                 {
                     ResetGameToMenu();
                 }
            }
        }
    }

    void HandleScanning(KinectManager km)
    {
        bool userStillThere = false;
        if (km.GetUserIdByIndex(0) == _pendingUserId) userStillThere = true; 

        if (!userStillThere && km.GetUsersCount() == 0) 
        {
            ResetToIdle();
            return;
        }
        
        curStateTimer += Time.deltaTime;

        if (enableVisualFeedback && _feedbackInstance != null)
        {
            float progress = Mathf.Clamp01(curStateTimer / lockInDuration);
            _feedbackInstance.UpdateProgress(progress);
        }

        if (curStateTimer >= lockInDuration)
        {
             km.SetPrimaryUserID(_pendingUserId);
             GameManager.Instance.StartGame(); 
             
             if (_feedbackInstance) _feedbackInstance.Hide();
             currentState = ExhibitionState.Gameplay;
             gameIdleTimer = 0f;
        }
    }

    void HandleGameplay(KinectManager km)
    {
        long primaryID = km.GetPrimaryUserID();
        bool isPrimaryTracked = km.IsUserTracked(primaryID);

        if (isPrimaryTracked)
        {
            // Happy Path
            gameIdleTimer = 0f;
            _recoveryTimer = 0f;
        }
        else
        {
            // OCCLUSION HANDLING (Grace Period)
            // Instead of instantly failing, we wait a bit.
            _recoveryTimer += Time.deltaTime;
            
            if (_recoveryTimer < recoveryGracePeriod)
            {
                // We are in grace period. Do nothing yet.
                // Optionally show a "!" icon?
                return; 
            }
            
            // Grace period over. Now counts as Abandonment.
            gameIdleTimer += Time.deltaTime;
            if (gameIdleTimer >= abandonTimeout)
            {
                ResetGameToMenu();
            }
        }
    }

    void HandleResultScreen(KinectManager km)
    {
        curStateTimer += Time.deltaTime;
        
        if (curStateTimer >= abandonTimeout)
        {
            ResetGameToMenu();
            return;
        }

        long primaryID = km.GetPrimaryUserID();
        bool isPrimaryTracked = km.IsUserTracked(primaryID);

        // Similar Occlusion logic for Result Screen?
        // If primary is lost, we might want to wait grace period before showing Warning.
        
        if (!isPrimaryTracked)
        {
             _recoveryTimer += Time.deltaTime;
             if (_recoveryTimer < recoveryGracePeriod) 
             {
                 // Wait for grace period before declaring lost
                 return;
             }
        
            // ACTIVATE WARNING
            if (_activeWarningText && !_activeWarningText.activeSelf) 
                _activeWarningText.SetActive(true);

            // SCAN FOR NEW PLAYERS USING SMART SELECTION
            long potentialNewID = GetBestUserInZone(km);
            
            if (potentialNewID != 0 && potentialNewID != primaryID)
            {
                if (_pendingUserId != potentialNewID)
                {
                   _pendingUserId = potentialNewID;
                   gameIdleTimer = 0f; 
                }
                
                gameIdleTimer += Time.deltaTime;
                
                if (enableVisualFeedback && _feedbackInstance != null)
                {
                    // Show lock-in for the new person
                    float progress = Mathf.Clamp01(gameIdleTimer / lockInDuration);
                    _feedbackInstance.UpdateProgress(progress);
                }

                if (gameIdleTimer >= lockInDuration)
                {
                    // SWAP USER
                    km.SetPrimaryUserID(potentialNewID);
                    if (_activeWarningText) _activeWarningText.SetActive(false);
                    if (_feedbackInstance) _feedbackInstance.Hide();
                    _recoveryTimer = 0f;
                }
            }
            else
            {
                gameIdleTimer = 0f;
                 if (_feedbackInstance) _feedbackInstance.Hide();
            }
        }
        else
        {
            // Primary is present.
            _recoveryTimer = 0f;
            if (_activeWarningText && _activeWarningText.activeSelf) 
                _activeWarningText.SetActive(false);
        }
    }
    
    void EnterResultState()
    {
        currentState = ExhibitionState.ResultScreen;
        curStateTimer = 0f;
        gameIdleTimer = 0f;
        _pendingUserId = 0;
        
        // Find Warning Text in the active canvas (Prefer Inspector References)
        _activeWarningText = null;
        if (winCanvas && winCanvas.activeSelf)
        {
             Debug.Log("ExhibitionManager: Win Canvas Detected Active.");
             if (winWarningText) _activeWarningText = winWarningText;
             else _activeWarningText = FindRecursive(winCanvas.transform, "Warning");
        }
        else if (loseCanvas && loseCanvas.activeSelf)
        {
             Debug.Log("ExhibitionManager: Lose Canvas Detected Active.");
             if (loseWarningText) {
                 _activeWarningText = loseWarningText;
                 Debug.Log("ExhibitionManager: Using Inspector-assigned Lose Warning Text.");
             }
             else {
                 _activeWarningText = FindRecursive(loseCanvas.transform, "Warning");
                 Debug.Log("ExhibitionManager: Searching recursively for 'Warning'. Found: " + (_activeWarningText != null));
             }
        }
        
        if (_activeWarningText) {
             _activeWarningText.SetActive(false);
             Debug.Log("ExhibitionManager: Warning Text assigned and initially disabled.");
        } else {
             Debug.LogError("ExhibitionManager: FAILED to find any Warning text object!");
        }
    }

    private GameObject FindRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child.gameObject;
            GameObject found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    void ResetToIdle()
    {
        currentState = ExhibitionState.Idle;
        _pendingUserId = 0;
        curStateTimer = 0f;
        if (_feedbackInstance) _feedbackInstance.Hide();
    }

    public void ResetGameToMenu()
    {
        Debug.Log($"Exhibition: Resetting to {mainMenuSceneName}...");
        if (string.IsNullOrEmpty(mainMenuSceneName)) 
            SceneManager.LoadScene("MainMenu");
        else 
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- HELPER METHODS ---

    private long GetBestUserInZone(KinectManager km)
    {
        // 1. Get ALL candidates
        int count = km.GetUsersCount();
        if (count == 0) return 0;

        long bestUser = 0;
        float bestDistanceSq = float.MaxValue;
        
        // 2. Iterate and Filter
        for (int i = 0; i < count; i++)
        {
            long userId = km.GetUserIdByIndex(i);
            if (userId == 0) continue;
            
            // Check Bounds
            if (IsUserInPlayZone(km, userId))
            {
                // Calculate Distance to (0,0,0) - or just Z really
                Vector3 pos = km.GetUserPosition(userId);
                float distSq = pos.x*pos.x + pos.z*pos.z; // Distance from center
                
                // Prioritize Closest
                if (distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    bestUser = userId;
                }
            }
        }
        
        return bestUser;
    }

    private bool IsUserInPlayZone(KinectManager km, long userId)
    {
        Vector3 pos = km.GetUserPosition(userId);
        
        // Z Depth Check
        if (pos.z < minZ || pos.z > maxZ) return false;
        
        // X Width Check (Symetric)
        if (Mathf.Abs(pos.x) > boundaryX) return false;
        
        return true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // Draw the Play Zone Box relative to the Kinect (assuming this script is near Kinect or 0,0,0)
        // Adjust center and size
        Vector3 center = new Vector3(0, 1.0f, (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(boundaryX * 2, 2.0f, maxZ - minZ);
        Gizmos.DrawWireCube(center, size);
    }
}
