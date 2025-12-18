using System.Collections;
using UnityEngine;
using Windows.Kinect;

/// <summary>
/// Persistent singleton that manages Kinect initialization and keeps it open across scenes.
/// This prevents the delay when switching scenes or restarting the game.
/// </summary>
public class KinectManager : MonoBehaviour
{
    private static KinectManager _instance;
    public static KinectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("KinectManager");
                _instance = go.AddComponent<KinectManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private KinectSensor sensor;
    private BodyFrameReader bodyFrameReader;
    private Body[] bodies;
    
    public bool IsInitialized { get; private set; }
    public bool IsReady { get; private set; }
    public KinectSensor Sensor => sensor;
    public BodyFrameReader BodyFrameReader => bodyFrameReader;
    public Body[] Bodies => bodies;

    void Awake()
    {
        // Ensure singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize Kinect early
        StartCoroutine(InitializeKinect());
    }

    private IEnumerator InitializeKinect()
    {
        Debug.Log("[KinectManager] Starting Kinect initialization...");
        IsInitialized = false;
        IsReady = false;

        // Get the sensor
        sensor = KinectSensor.GetDefault();
        if (sensor == null)
        {
            Debug.LogError("[KinectManager] KinectSensor.GetDefault() returned null. Kinect not connected?");
            yield break;
        }

        // Open the sensor first (IsAvailable becomes true AFTER Open())
        if (!sensor.IsOpen)
        {
            sensor.Open();
            Debug.Log("[KinectManager] Kinect sensor opened. Waiting for availability...");
        }

        // Wait for sensor to become available (with timeout)
        float waitTime = Time.realtimeSinceStartup + 5f; // 5 second timeout
        while (!sensor.IsAvailable && Time.realtimeSinceStartup < waitTime)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!sensor.IsAvailable)
        {
            Debug.LogWarning("[KinectManager] Kinect sensor did not become available within timeout period.");
            yield break;
        }

        // Wait for sensor to be fully open
        waitTime = Time.realtimeSinceStartup + 3f;
        while (!sensor.IsOpen && Time.realtimeSinceStartup < waitTime)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!sensor.IsOpen)
        {
            Debug.LogWarning("[KinectManager] Kinect sensor did not open within timeout period.");
            yield break;
        }

        // Create body frame reader
        bodyFrameReader = sensor.BodyFrameSource.OpenReader();
        bodies = new Body[sensor.BodyFrameSource.BodyCount];

        IsInitialized = true;
        IsReady = true;
        Debug.Log("[KinectManager] Kinect initialized and ready! Sensor is open: " + sensor.IsOpen + ", Available: " + sensor.IsAvailable);
    }

    /// <summary>
    /// Wait for Kinect to be ready before proceeding.
    /// Use this in Start() methods that depend on Kinect.
    /// </summary>
    public IEnumerator WaitForReady()
    {
        while (!IsReady)
        {
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("[KinectManager] Kinect is ready!");
    }

    /// <summary>
    /// Check if Kinect is ready (non-coroutine version for Update loops)
    /// </summary>
    public bool CheckReady()
    {
        if (!IsInitialized || sensor == null || !sensor.IsOpen || !sensor.IsAvailable)
        {
            return false;
        }
        return IsReady;
    }

    void OnDestroy()
    {
        // Only close if this is the actual instance being destroyed
        if (_instance == this)
        {
            Cleanup();
        }
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        Debug.Log("[KinectManager] Cleaning up Kinect resources...");
        
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }

        // Note: We intentionally DON'T close the sensor here to keep it warm across scenes
        // The sensor will be closed when the application quits
        if (sensor != null && sensor.IsOpen)
        {
            // Only close on application quit, not on scene changes
            if (Application.isEditor || !Application.isPlaying)
            {
                sensor.Close();
                Debug.Log("[KinectManager] Kinect sensor closed.");
            }
        }

        IsInitialized = false;
        IsReady = false;
    }

    /// <summary>
    /// Manually close the sensor (use sparingly, only when absolutely necessary)
    /// </summary>
    public void CloseSensor()
    {
        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
            IsReady = false;
            Debug.Log("[KinectManager] Kinect sensor manually closed.");
        }
    }

    /// <summary>
    /// Reinitialize the Kinect (useful if connection was lost)
    /// </summary>
    public void Reinitialize()
    {
        Cleanup();
        StartCoroutine(InitializeKinect());
    }
}

