using UnityEngine;
using System.Collections;

public class SciFiPanelFX : MonoBehaviour
{
    [Header("Animation Settings")]
    public float openDuration = 0.4f;
    public bool animateHorizontalFirst = true; // Sci-fi "TV turn on" effect

    [Header("Glitch Settings")]
    public float glitchChance = 0.05f;
    public float shakeIntensity = 5f;

    private RectTransform rectTransform;
    private Vector2 originalPos;
    private Vector3 originalScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;
        originalScale = Vector3.one; // Assuming final scale is 1,1,1
    }

    public void PlayOpenAnimation()
    {
        // Reset to invisible
        rectTransform.localScale = new Vector3(0.01f, 0.01f, 1f); 
        StartCoroutine(AnimatePopUp());
    }

    private IEnumerator AnimatePopUp()
    {
        float timer = 0f;

        while (timer < openDuration)
        {
            // CRITICAL: Use unscaledDeltaTime so this runs while game is paused
            timer += Time.unscaledDeltaTime; 
            float progress = timer / openDuration;

            // Add a slight "overshoot" curve for energy
            float curve = Mathf.Sin(progress * Mathf.PI * 0.5f); 

            if (animateHorizontalFirst)
            {
                // Expand Width first, then Height
                float width = Mathf.Clamp01(curve * 2f);
                float height = Mathf.Clamp01((curve - 0.5f) * 2f);
                rectTransform.localScale = new Vector3(width, height, 1f);
            }
            else
            {
                rectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, curve);
            }

            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    void Update()
    {
        // Only glitch if the panel is fully open
        if (rectTransform.localScale.x > 0.9f)
        {
            if (Random.value < glitchChance) // Use value for randomness
            {
                PerformGlitch();
            }
            else
            {
                ResetGlitch();
            }
        }
    }

    void PerformGlitch()
    {
        float x = Random.Range(-shakeIntensity, shakeIntensity);
        float y = Random.Range(-shakeIntensity, shakeIntensity);
        
        // Jitter position
        rectTransform.anchoredPosition = originalPos + new Vector2(x, y);
    }

    void ResetGlitch()
    {
        rectTransform.anchoredPosition = originalPos;
    }
}