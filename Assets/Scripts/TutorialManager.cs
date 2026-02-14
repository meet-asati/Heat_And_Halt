using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;
    
    // NEW: Reference to the FX script
    public SciFiPanelFX visualEffects; 

    private bool isTutorialActive = false;
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
            // We use unscaled time logic here because TimeScale is 0
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                DismissTutorial();
            }
        }
    }

    public void ShowTutorial(string message, System.Action onDismiss = null)
    {
        if (tutorialPanel == null) return;

        tutorialText.text = message;
        tutorialPanel.SetActive(true);
        
        // NEW: Trigger the Sci-Fi Animation
        if (visualEffects != null)
        {
            visualEffects.PlayOpenAnimation();
        }

        // Save the action for later
        this.onTutorialClosed = onDismiss;

        // Pause the game
        Time.timeScale = 0f; 
        isTutorialActive = true;
    }

    public void DismissTutorial()
    {
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f; 
        isTutorialActive = false;
        
        // Execute the stored action (Wake up the boss!)
        if (onTutorialClosed != null)
        {
            onTutorialClosed.Invoke();
            onTutorialClosed = null; 
        }
    }
}