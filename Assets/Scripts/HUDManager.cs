using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the Red 'Heat' Fill Image here")]
    public Image heatBarFill;
    [Tooltip("Drag the Blue 'Freeze' Fill Image here")]
    public Image freezeBarFill;

    [Header("Heat Visual Settings")]
    public Color normalHeatColor = new Color(1f, 0.5f, 0f); // Orange
    public Color criticalHeatColor = Color.red;
    [Tooltip("Heat percentage (0-1) where flashing starts")]
    [Range(0f, 1f)] public float heatCriticalThreshold = 0.8f;
    [SerializeField] float flashSpeed = 10f;

    [Header("Freeze Visual Settings")]
    public Color normalFreezeColor = Color.cyan;
    public Color lowEnergyColor = Color.gray;
    [Tooltip("Energy percentage (0-1) where bar looks empty")]
    [Range(0f, 1f)] public float energyLowThreshold = 0.2f;

    // Singleton instance for easy access
    public static HUDManager Instance;

    [Header("Boss UI")]
    public GameObject bossHealthContainer; // The Background Object (to show/hide)
    public Image bossHealthFill;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateHeatBar(float currentHeat, float maxHeat)
    {
        if (heatBarFill != null)
        {
            float ratio = currentHeat / maxHeat;
            heatBarFill.fillAmount = ratio;

            // Visual Logic: Flash if critical
            if (ratio >= heatCriticalThreshold)
            {
                // PingPong creates a back-and-forth value between 0 and 1 over time
                float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);
                heatBarFill.color = Color.Lerp(normalHeatColor, criticalHeatColor, flash);
            }
            else
            {
                heatBarFill.color = normalHeatColor;
            }
        }
    }

    public void UpdateFreezeBar(float currentEnergy, float maxEnergy)
    {
        if (freezeBarFill != null)
        {
            float ratio = currentEnergy / maxEnergy;
            freezeBarFill.fillAmount = ratio;

            // Visual Logic: Dim if low energy to show "Recharging" state
            if (ratio <= energyLowThreshold)
            {
                freezeBarFill.color = Color.Lerp(lowEnergyColor, normalFreezeColor, ratio / energyLowThreshold);
            }
            else
            {
                freezeBarFill.color = normalFreezeColor;
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