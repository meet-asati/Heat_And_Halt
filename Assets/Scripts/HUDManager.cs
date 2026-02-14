using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Heat Bar Settings")]
    public Slider heatSlider;       // Drag the slider component here
    public Image heatFillImage;     // Drag the "Fill" image inside the slider here (for color changing)
    
    [Header("Heat Visuals")]
    public Color normalHeatColor = new Color(1f, 0.5f, 0f); // Orange
    public Color criticalHeatColor = Color.red;
    [Range(0f, 1f)] public float heatCriticalThreshold = 0.8f;
    [SerializeField] float flashSpeed = 10f;

    [Header("Frost Bar Settings")]
    public Slider frostSlider;      // Drag the slider component here
    public Image frostFillImage;    // Drag the "Fill" image inside the slider here
    
    [Header("Frost Visuals")]
    public Color normalFreezeColor = Color.cyan;
    public Color lowEnergyColor = Color.gray;
    [Range(0f, 1f)] public float energyLowThreshold = 0.2f;

    [Header("Boss UI")]
    public GameObject bossHealthContainer;
    public Image bossHealthFill;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateHeatBar(float currentHeat, float maxHeat)
    {
        // 1. Update the Slider Value
        if (heatSlider != null)
        {
            heatSlider.value = currentHeat / maxHeat;
        }

        // 2. Update the Color (Flashing Effect)
        if (heatFillImage != null)
        {
            float ratio = currentHeat / maxHeat;
            if (ratio >= heatCriticalThreshold)
            {
                // PingPong creates a flashing effect
                float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
                heatFillImage.color = Color.Lerp(normalHeatColor, criticalHeatColor, flash);
            }
            else
            {
                heatFillImage.color = normalHeatColor;
            }
        }
    }

    public void UpdateFreezeBar(float currentEnergy, float maxEnergy)
    {
        // 1. Update the Slider Value
        if (frostSlider != null)
        {
            frostSlider.value = currentEnergy / maxEnergy;
        }

        // 2. Update the Color (Dimming Effect)
        if (frostFillImage != null)
        {
            float ratio = currentEnergy / maxEnergy;
            
            // If energy is very low, turn gray to indicate "Recharging"
            if (ratio <= energyLowThreshold && ratio > 0.01f)
            {
                frostFillImage.color = Color.Lerp(lowEnergyColor, normalFreezeColor, ratio / energyLowThreshold);
            }
            else
            {
                frostFillImage.color = normalFreezeColor;
            }
        }
    }

    public void ShowBossHealth(bool show)
    {
        if (bossHealthContainer != null) bossHealthContainer.SetActive(show);
    }

    public void UpdateBossHealth(float currentHealth, float maxHealth)
    {
        if (bossHealthFill != null)
        {
            bossHealthFill.fillAmount = currentHealth / maxHealth;
        }
    }
}