using UnityEngine;
using TMPro;
using Windows.Kinect;

/// <summary>
/// Debug overlay to show Kinect status in real-time.
/// Helps diagnose "is Kinect reading me or not?" issues.
/// </summary>
public class KinectDebugOverlay : MonoBehaviour
{
    [Header("Debug Display")]
    [Tooltip("Text component to display Kinect status (optional - will create if not set)")]
    public TextMeshProUGUI statusText;

    [Tooltip("Show debug overlay")]
    public bool showDebug = true;

    private KinectSensor sensor;
    private BodyFrameReader bodyFrameReader;
    private Body[] bodies;
    private int trackedBodiesCount = 0;
    private float updateInterval = 0.5f; // Update every 0.5 seconds
    private float lastUpdateTime = 0f;

    void Start()
    {
        // Get or create text component
        if (statusText == null)
        {
            GameObject textObj = new GameObject("KinectDebugText");
            textObj.transform.SetParent(transform);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(400, 200);

            statusText = textObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 16;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.TopLeft;
        }

        // Try to get sensor reference
        sensor = KinectSensor.GetDefault();
        if (sensor != null && sensor.IsOpen)
        {
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            bodies = new Body[sensor.BodyFrameSource.BodyCount];
        }
    }

    void Update()
    {
        if (!showDebug || statusText == null) return;

        // Update at intervals to avoid spam
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        // Refresh sensor reference if needed
        if (sensor == null)
        {
            sensor = KinectSensor.GetDefault();
            if (sensor != null && sensor.IsOpen && bodyFrameReader == null)
            {
                bodyFrameReader = sensor.BodyFrameSource.OpenReader();
                bodies = new Body[sensor.BodyFrameSource.BodyCount];
            }
        }

        // Count tracked bodies
        trackedBodiesCount = 0;
        if (bodyFrameReader != null && sensor != null && sensor.IsOpen)
        {
            using (var frame = bodyFrameReader.AcquireLatestFrame())
            {
                if (frame != null && bodies != null)
                {
                    frame.GetAndRefreshBodyData(bodies);
                    foreach (var body in bodies)
                    {
                        if (body != null && body.IsTracked)
                        {
                            trackedBodiesCount++;
                        }
                    }
                }
            }
        }

        // Build status string
        string status = "=== KINECT STATUS ===\n";
        
        if (sensor == null)
        {
            status += "Sensor: NULL (not found)\n";
        }
        else
        {
            status += $"Sensor: Found\n";
            status += $"IsOpen: {sensor.IsOpen}\n";
            status += $"IsAvailable: {sensor.IsAvailable}\n";
        }

        if (bodyFrameReader == null)
        {
            status += "Reader: NULL\n";
        }
        else
        {
            status += "Reader: OK\n";
        }

        status += $"Tracked Bodies: {trackedBodiesCount}\n";

        // Status indicator
        if (sensor != null && sensor.IsOpen && sensor.IsAvailable && trackedBodiesCount > 0)
        {
            status += "\n✓ KINECT IS READING YOU";
        }
        else if (sensor != null && sensor.IsOpen && sensor.IsAvailable)
        {
            status += "\n⚠ Kinect ready but no bodies tracked";
        }
        else if (sensor != null && sensor.IsOpen)
        {
            status += "\n⚠ Kinect open but not available yet";
        }
        else
        {
            status += "\n✗ Kinect not ready";
        }

        statusText.text = status;
    }

    void OnDestroy()
    {
        // Don't dispose reader here - it might be shared
        bodyFrameReader = null;
        sensor = null;
        bodies = null;
    }
}

