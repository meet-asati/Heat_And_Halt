using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the Red 'Heat' Fill Image here")]
    public Image heatBarFill;
    
    [Tooltip("Drag the Blue 'Freeze' Fill Image here")]
    public Image freezeBarFill;

    // Singleton instance for easy access
    public static HUDManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateHeatBar(float currentHeat, float maxHeat)
    {
        if (heatBarFill != null)
        {
            // Calculate percentage (0 to 1)
            heatBarFill.fillAmount = currentHeat / maxHeat;
        }
    }

    public void UpdateFreezeBar(float currentEnergy, float maxEnergy)
    {
        if (freezeBarFill != null)
        {
            freezeBarFill.fillAmount = currentEnergy / maxEnergy;
        }
    }
}
