using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;

    private bool isTutorialActive = false;
    
    // NEW: Variable to store "What to do next"
    private System.Action onTutorialClosed;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    void Update()
    {
        if (isTutorialActive)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                DismissTutorial();
            }
        }
    }

    // UPDATED: Now accepts an optional "onDismiss" action
    public void ShowTutorial(string message, System.Action onDismiss = null)
    {
        if (tutorialPanel == null) return;

        tutorialText.text = message;
        tutorialPanel.SetActive(true);
        
        // Save the action for later
        this.onTutorialClosed = onDismiss;

        Time.timeScale = 0f; 
        isTutorialActive = true;
    }

    public void DismissTutorial()
    {
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f; 
        isTutorialActive = false;
        
        // NEW: Execute the stored action (Wake up the boss!)
        if (onTutorialClosed != null)
        {
            onTutorialClosed.Invoke();
            onTutorialClosed = null; // Clear it so it doesn't run again
        }
    }
}