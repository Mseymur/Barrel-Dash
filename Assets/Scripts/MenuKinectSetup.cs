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
        // Simply accessing the Instance property will force the KinectSensorManager 
        // to be created and start initializing the sensor.
        // This ensures the sensor is "warming up" while the user is in the menu.
        Debug.Log("[MenuKinectSetup] Requesting KinectSensorManager initialization for Main Menu...");
        var manager = KinectSensorManager.Instance; 
        
        if (manager != null)
        {
             Debug.Log("[MenuKinectSetup] KinectSensorManager is active.");
        }
    }
}

