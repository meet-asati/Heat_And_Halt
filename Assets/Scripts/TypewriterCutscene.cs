using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem; // REQUIRED for the new system

public class TypewriterCutscene : MonoBehaviour
{
    [Header("Visuals")]
    public Image backgroundDisplay;
    public Sprite cutsceneImage;

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip typingClip;

    [Header("Story Content")]
    [TextArea(3, 10)] 
    public string[] storyLines;
    public float typingSpeed = 0.05f;

    [Header("Navigation")]
    [Tooltip("Leave empty to auto-load next index")]
    public string overrideSceneName; 

    [Header("UI References")]
    public TextMeshProUGUI storyTextComponent;
    public GameObject continuePrompt;

    private int currentLineIndex = 0;
    private bool isTyping = false;

    void Start()
    {
        // 1. Setup Image
        if (backgroundDisplay != null && cutsceneImage != null)
            backgroundDisplay.sprite = cutsceneImage;

        // 2. Start Music
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }

        // 3. Start Text
        if (storyTextComponent != null)
        {
            storyTextComponent.text = "";
            StartCoroutine(TypeLine(storyLines[0]));
        }

        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    void Update()
    {
        // --- FIX: USE NEW INPUT SYSTEM HERE ---
        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (leftClick || spacePressed)
        {
            if (isTyping)
            {
                // SKIP: Finish line instantly
                StopAllCoroutines();
                storyTextComponent.text = storyLines[currentLineIndex];
                if (sfxSource != null) sfxSource.Stop();
                isTyping = false;
                if (continuePrompt != null) continuePrompt.SetActive(true);
            }
            else
            {
                // NEXT: Go to next line
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

    void LoadNextLevel()
    {
        // 1. Check for Override
        if (!string.IsNullOrEmpty(overrideSceneName))
        {
            Debug.Log($"Loading Override Scene: '{overrideSceneName}'");
            SceneManager.LoadScene(overrideSceneName);
            return;
        }

        // 2. Auto-Load Next Index
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        int totalScenes = SceneManager.sceneCountInBuildSettings;

        if (nextSceneIndex < totalScenes)
        {
            Debug.Log($"Loading Scene Index {nextSceneIndex}...");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // Fallback to loop to start
            Debug.Log("End of Build List. Looping to 0.");
            SceneManager.LoadScene(0);
        }
    }
}