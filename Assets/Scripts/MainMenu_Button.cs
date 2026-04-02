using UnityEngine;
using UnityEngine.SceneManagement; 
public class MainMenu : MonoBehaviour
{
    public void LoadLevel(string levelName)
    {
        Debug.Log("Loading scene: " + levelName);
        SceneManager.LoadScene(levelName);
    }

    public void QuitGame()
    {
        Debug.Log("Game is exiting...");
        Application.Quit();
    }
}