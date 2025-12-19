using UnityEngine;
using System.Collections;

/// <summary>
/// Helper script to ensure Kinect is initialized in the Main Menu for hand gesture control.
/// Attach this to any GameObject in the Main Menu scene.
/// </summary>
public class MenuKinectSetup : MonoBehaviour
{
    void Start()
    {
        // Simply accessing the Instance property ensures we have a reference.
        // The KinectManager should be present in the scene (as a Prefab).
        Debug.Log("[MenuKinectSetup] Checking KinectManager for Main Menu...");
        
        var manager = KinectManager.Instance; 
        
        if (manager != null)
        {
             Debug.Log("[MenuKinectSetup] KinectManager is active.");
             // Ensure gestures are ON in the menu
             if (GameManager.Instance != null)
             {
                 GameManager.Instance.SetGesturesActive(true);
             }
        }
        else
        {
            Debug.LogWarning("[MenuKinectSetup] KinectManager not found! Please verify the 'KinectController' prefab is in the scene.");
        }
    }
}

