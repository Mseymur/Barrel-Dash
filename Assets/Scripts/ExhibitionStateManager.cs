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

    [Header("References")]
    public LockInFeedback lockInFeedbackPrefab; 
    
    // We observe these to know when to enter "Result" state
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
        if (km.GetUsersCount() > 0)
        {
            long userId = km.GetPrimaryUserID();
            if (userId == 0) userId = km.GetUserIdByIndex(0);

            if (userId != 0)
            {
                _pendingUserId = userId;
                curStateTimer = 0f;
                currentState = ExhibitionState.Scanning;
            }
        }
        else
        {
            // Abandonment Logic for Pre-Game (Wait State)
            // Only if we are NOT in the Main Menu already
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

        if (!isPrimaryTracked)
        {
            gameIdleTimer += Time.deltaTime;
            if (gameIdleTimer >= abandonTimeout)
            {
                ResetGameToMenu();
            }
        }
        else
        {
            gameIdleTimer = 0f;
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

        if (!isPrimaryTracked)
        {
            if (_activeWarningText && !_activeWarningText.activeSelf) 
                _activeWarningText.SetActive(true);

            long potentialNewID = km.GetUserIdByIndex(0);
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
                    float progress = Mathf.Clamp01(gameIdleTimer / lockInDuration);
                    _feedbackInstance.UpdateProgress(progress);
                }

                if (gameIdleTimer >= lockInDuration)
                {
                    km.SetPrimaryUserID(potentialNewID);
                    if (_activeWarningText) _activeWarningText.SetActive(false);
                    if (_feedbackInstance) _feedbackInstance.Hide();
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
}
