using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("Name of the scene to load when Play is pressed")]
    public string firstLevelName = "Cutscene_Intro"; 

    [Header("UI Panels")]
    public GameObject assetsPanel; // Drag your 'AssetsPanel' here
    public GameObject mainButtonsGroup; // Optional: Drag the parent of your 3 buttons here

    public void PlayGame()
    {
        Debug.Log("Starting Sequence...");
        // Ensure the DJ knows to change music if needed
        // if (MusicManager.Instance != null)
        // {
        //     // Optional: You could fade music here
        // }
        SceneManager.LoadScene(firstLevelName);
    }

    public void ShowAssets(bool show)
    {
        // If true, show Assets and hide Buttons.
        // If false, hide Assets and show Buttons.
        
        if (assetsPanel != null) 
            assetsPanel.SetActive(show);
            
        if (mainButtonsGroup != null) 
            mainButtonsGroup.SetActive(!show);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting...");
        Application.Quit();
    }
}