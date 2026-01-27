using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // Required for New Input System

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;

    private bool isTutorialActive = false;

    void Awake()
    {
        // Singleton setup (allows Triggers to find this easily)
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    void Update()
    {
        // Only listen for input if a tutorial is currently showing
        if (isTutorialActive)
        {
            // Check for Enter Key (New Input System)
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                DismissTutorial();
            }
        }
    }

    public void ShowTutorial(string message)
    {
        if (tutorialPanel == null) return;

        tutorialText.text = message;
        tutorialPanel.SetActive(true);
        
        // PAUSE THE GAME
        Time.timeScale = 0f; 
        isTutorialActive = true;
    }

    public void DismissTutorial()
    {
        tutorialPanel.SetActive(false);
        
        // RESUME THE GAME
        Time.timeScale = 1f; 
        isTutorialActive = false;
    }
}