using UnityEngine;
using UnityEngine.SceneManagement;

public class EndMenu : MonoBehaviour
{
    public void RestartGame()
    {
        // Signal that this is a restart
        GameManager.IsRestarting = true;
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void GoToMainMenu()
    {
        // Replace "MainMenu" with the name of your Main Menu scene
        SceneManager.LoadScene("MainMenu");
    }
}