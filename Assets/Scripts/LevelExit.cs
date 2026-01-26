using UnityEngine;
using UnityEngine.SceneManagement; // Required for changing levels

public class LevelExit : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Name of the scene to load. Leave empty to load the next scene in Build Settings.")]
    public string nextSceneName = ""; 

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is the Player
        if (other.CompareTag("Player"))
        {
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        Debug.Log("Exiting Level...");

        if (nextSceneName != "")
        {
            // Load specific scene by name
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Load the next scene in the Build Settings list
            // (Current Index + 1)
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
    }
}