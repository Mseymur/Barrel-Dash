using UnityEngine;
using Windows.Kinect;

public class KinectDebugUI : MonoBehaviour
{
    private GUIStyle style;

    void Start()
    {
        style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Bold;
    }

    void OnGUI()
    {
        if (KinectSensorManager.Instance == null)
        {
            GUI.Label(new Rect(10, 10, 500, 30), "Status: NO MANAGER FOUND", style);
            return;
        }

        KinectSensor sensor = KinectSensorManager.Instance.Sensor;
        
        string status = "Initializing...";
        string readerStatus = "Null";

        if (sensor != null)
        {
            status = $"Open: {sensor.IsOpen} | Available: {sensor.IsAvailable}";
        }

        if (KinectSensorManager.Instance.BodyFrameReader != null)
        {
            readerStatus = "Active";
        }

        GUI.Label(new Rect(10, 10, 800, 40), $"Kinect Status: {status}", style);
        GUI.Label(new Rect(10, 50, 800, 40), $"Frame Reader: {readerStatus}", style);
        GUI.Label(new Rect(10, 90, 800, 40), $"Time Since Load: {Time.timeSinceLevelLoad:F2}", style);
    }
}