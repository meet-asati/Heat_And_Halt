using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class TypewriterCutscene : MonoBehaviour
{
    [Header("Visuals")]
    public Image backgroundDisplay;
    public Sprite cutsceneImage;

    [Header("Ending Twist (The Scare)")]
    public Sprite enemyTwistImage;   // Drag the scary Enemy image here
    public TMP_FontAsset scaryFont;  // Drag a different font here (optional)
    public Color scaryColor = Color.red; // Text turns red
    [Tooltip("The specific phrase that triggers the change")]
    public string triggerPhrase = "could it happen again?";

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip typingClip;
    public AudioClip scareSound; // Optional: A sudden boom or glitch sound

    [Header("Story Content")]
    [TextArea(3, 10)] 
    public string[] storyLines;
    public float typingSpeed = 0.05f;

    [Header("Navigation")]
    public string overrideSceneName; 

    [Header("UI References")]
    public TextMeshProUGUI storyTextComponent;
    public GameObject continuePrompt;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private TMP_FontAsset originalFont;
    private Color originalColor;

    void Start()
    {
        // Save original styles so we can reset if needed (good practice)
        if (storyTextComponent != null)
        {
            originalFont = storyTextComponent.font;
            originalColor = storyTextComponent.color;
            storyTextComponent.text = "";
        }

        if (backgroundDisplay != null && cutsceneImage != null)
            backgroundDisplay.sprite = cutsceneImage;

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }

        if (storyLines.Length > 0)
            StartCoroutine(TypeLine(storyLines[0]));

        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    void Update()
    {
        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (leftClick || spacePressed)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                storyTextComponent.text = storyLines[currentLineIndex];
                if (sfxSource != null) sfxSource.Stop();
                isTyping = false;
                if (continuePrompt != null) continuePrompt.SetActive(true);
                
                // Ensure twist applies even if skipped
                CheckForTwist(storyLines[currentLineIndex]); 
            }
            else
            {
                NextLine();
            }
        }
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < storyLines.Length)
        {
            if (continuePrompt != null) continuePrompt.SetActive(false);
            storyTextComponent.text = "";
            StartCoroutine(TypeLine(storyLines[currentLineIndex]));
        }
        else
        {
            LoadNextLevel();
        }
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        
        // 1. CHECK FOR THE TWIST BEFORE TYPING
        CheckForTwist(line);

        if (sfxSource != null && typingClip != null)
        {
            sfxSource.clip = typingClip;
            sfxSource.loop = true;
            sfxSource.Play();
        }

        foreach (char letter in line.ToCharArray())
        {
            storyTextComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (sfxSource != null) sfxSource.Stop();
        isTyping = false;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    void CheckForTwist(string line)
    {
        // If the line contains the scary phrase...
        if (line.ToLower().Contains(triggerPhrase.ToLower()))
        {
            // ...Swap the Image
            if (enemyTwistImage != null) 
                backgroundDisplay.sprite = enemyTwistImage;
            
            // ...Change Font & Color
            if (scaryFont != null) 
                storyTextComponent.font = scaryFont;
            
            storyTextComponent.color = scaryColor;

            // ...Stop Music for dramatic effect?
            if (musicSource != null) musicSource.Stop();

            // ...Play Scare Sound?
            if (sfxSource != null && scareSound != null) 
                sfxSource.PlayOneShot(scareSound);
        }
    }

    void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(overrideSceneName))
        {
            SceneManager.LoadScene(overrideSceneName);
            return;
        }
        SceneManager.LoadScene(0); // Default to menu
    }
}