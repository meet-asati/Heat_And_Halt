using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class GameEndingConsole : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string promptMessage = "[E] PREVENT SABOTAGE";
    public string winMessage = "SABOTAGE PREVENTED.\n\nSYSTEM REBOOTING...";
    
    [Header("Scene Management")]
    [Tooltip("Type the EXACT name of your Ending Cutscene here")]
    public string nextSceneName = "Cutscene_Outro"; 

    [Header("UI References")]
    public GameObject interactPrompt;

    private bool isPlayerNearby = false;
    private bool gameEnded = false;

    void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (gameEnded) return;

        if (isPlayerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            EndGameSequence();
        }
    }

    void EndGameSequence()
    {
        gameEnded = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // Show the Victory Message
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowTutorial(winMessage);
        }

        // Wait for player to press Enter, then load the Cutscene
        StartCoroutine(WaitAndLoadCutscene());
    }

    System.Collections.IEnumerator WaitAndLoadCutscene()
    {
        // Wait here as long as the game is paused (meaning the message is still on screen)
        while (Time.timeScale == 0f)
        {
            yield return null; 
        }

        Debug.Log($"Loading Ending Cutscene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactPrompt != null) interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}