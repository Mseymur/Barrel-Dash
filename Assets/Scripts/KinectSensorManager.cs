using System.Collections;
using UnityEngine;
using Windows.Kinect;

/// <summary>
/// Persistent singleton that manages Kinect initialization and keeps it open across scenes.
/// Fixes the power-cycle issue by owning the Sensor lifetime.
/// </summary>
public class KinectSensorManager : MonoBehaviour
{
    private static KinectSensorManager _instance;
    public static KinectSensorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find it in the scene first
                _instance = FindObjectOfType<KinectSensorManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("KinectSensorManager");
                    _instance = go.AddComponent<KinectSensorManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private KinectSensor sensor;
    private BodyFrameReader bodyFrameReader;
    
    public bool IsInitialized { get; private set; }
    public bool IsReady { get; private set; }
    public KinectSensor Sensor => sensor;
    public BodyFrameReader BodyFrameReader => bodyFrameReader;

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
        // If we already have a sensor from a previous scene, skip init
        if (sensor != null && sensor.IsOpen)
        {
            IsInitialized = true;
            IsReady = true;
            yield break;
        }

        Debug.Log("[KinectSensorManager] Starting Kinect initialization...");
        IsInitialized = false;
        IsReady = false;

        sensor = KinectSensor.GetDefault();
        if (sensor == null)
        {
            Debug.LogError("[KinectSensorManager] KinectSensor.GetDefault() returned null.");
            yield break;
        }

        if (!sensor.IsOpen)
        {
            sensor.Open();
        }

        // Wait for sensor to be available
        float waitTime = Time.realtimeSinceStartup + 5f;
        while (!sensor.IsAvailable && Time.realtimeSinceStartup < waitTime)
        {
            yield return null;
        }

        // Open the reader ONCE here. Player scripts will borrow it.
        bodyFrameReader = sensor.BodyFrameSource.OpenReader();
        
        IsInitialized = true;
        IsReady = true;
        Debug.Log("[KinectSensorManager] Kinect initialized and ready.");
    }

    public IEnumerator WaitForReady()
    {
        while (!IsReady)
        {
            yield return null;
        }
    }

    // --- THE FIX: Handle Cleanup Correctly ---

    void OnDestroy()
    {
        // Do NOTHING here regarding the sensor.
        // When the scene changes, this object persists. 
        // If a duplicate is destroyed, it shouldn't close the sensor.
    }

    void OnApplicationQuit()
    {
        // Only close the sensor when the actual application closes.
        if (bodyFrameReader != null)
        {
            bodyFrameReader.Dispose();
            bodyFrameReader = null;
        }

        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
            Debug.Log("[KinectSensorManager] Application quitting. Sensor closed.");
        }
    }
}