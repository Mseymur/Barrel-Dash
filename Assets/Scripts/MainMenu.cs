using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{  
    
    public AudioClip menuMusic; // Drag your audio clip here in the Inspector
    private AudioSource audioSource;

    void Start()
    {
        // Initialize KinectManager early so it's ready when the game starts
        // This prevents delays when loading the game scene
        // The KinectManager initializes automatically in Awake(), so just accessing Instance starts it
        KinectManager kinectManager = KinectManager.Instance;
        
        // Ensure there's an AudioSource component attached to the GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Assign the audio clip to the AudioSource
        audioSource.clip = menuMusic;
        audioSource.loop = true; // Loop the music
        audioSource.playOnAwake = true; // Optional: Play automatically
        audioSource.Play(); // Play the music
    }
    
    public void StartGame(string sceneName)
    {
        Debug.Log("Start Game clicked!");
        SceneManager.LoadScene(sceneName); // Replace with your game scene name
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game clicked!");
        Application.Quit(); // Works only in a built game
    }
}