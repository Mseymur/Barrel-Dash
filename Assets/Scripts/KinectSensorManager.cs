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
    private Body[] bodies;
    
    public bool IsInitialized { get; private set; }
    public bool IsReady { get; private set; }
    public KinectSensor Sensor => sensor;
    public BodyFrameReader BodyFrameReader => bodyFrameReader;
    public Body[] Bodies => bodies;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitializeKinect());
    }

    private IEnumerator InitializeKinect()
    {
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

        if (sensor.IsAvailable)
        {
             Debug.Log("[KinectSensorManager] Sensor is Available.");
        }
        else
        {
             Debug.LogWarning("[KinectSensorManager] Sensor is NOT Available after timeout.");
        }

        // Open the reader ONCE here.
        if (bodyFrameReader == null)
        {
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
        }

        if (bodies == null)
        {
            bodies = new Body[sensor.BodyFrameSource.BodyCount];
        }
        
        IsInitialized = true;
        IsReady = true;
        Debug.Log($"[KinectSensorManager] Ready. Sensor Open: {sensor.IsOpen}, Available: {sensor.IsAvailable}");
    }

    public IEnumerator WaitForReady()
    {
        while (!IsReady)
        {
            yield return null;
        }
    }

    void OnDestroy()
    {
        // Do NOT close sensor here. This object persists.
    }

    void OnApplicationQuit()
    {
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
    
    void OnGUI()
    {
        // Simple debug overlay
        if (sensor != null)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Sensor Open: {sensor.IsOpen}");
            GUILayout.Label($"Sensor Available: {sensor.IsAvailable}");
            
            int trackedCount = 0;
            if (bodies != null)
            {
                foreach(var b in bodies)
                {
                    if(b != null && b.IsTracked) trackedCount++;
                }
            }
            GUILayout.Label($"Tracked Bodies: {trackedCount}");
            GUILayout.EndArea();
        }
    }
}