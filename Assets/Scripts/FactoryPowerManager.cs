using UnityEngine;
using UnityEngine.Rendering; // Required for AmbientMode

public class FactoryPowerManager : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Assign the Popup GameObject (Text/Image) here.")]
    public GameObject powerOutagePopup; 

    [Header("Lighting Settings")]
    // RGB (60, 60, 60)
    public Color blackoutColor = new Color32(60, 60, 60, 255); 
    
    // Store the original skybox material to restore it later
    private Material defaultSkybox;

    void Start()
    {
        // Save the current skybox so we can restore it later
        defaultSkybox = RenderSettings.skybox;

        // Ensure popup is hidden at start
        if (powerOutagePopup != null)
            powerOutagePopup.SetActive(false);
    }

    // Call this function when the First Fuse Box is destroyed
    public void TriggerBlackout()
    {
        // 1. Change Mode from Skybox to Color (Flat)
        RenderSettings.ambientMode = AmbientMode.Flat;
        
        // 2. Set the Ambient Color to Dark Gray (60, 60, 60)
        RenderSettings.ambientLight = blackoutColor;

        // 3. Show the Popup
        if (powerOutagePopup != null)
        {
            powerOutagePopup.SetActive(true);
            // Optional: Hide popup after 5 seconds
            Invoke("HidePopup", 5f); 
        }

        Debug.Log("Factory Power: BLACKOUT TRIGGERED");
        
        // Force lighting update
        DynamicGI.UpdateEnvironment();
    }

    // Call this function when the Second Fuse Box (Generator) is destroyed
    public void RestorePower()
    {
        // 1. Restore the original Skybox Material
        RenderSettings.skybox = defaultSkybox;

        // 2. Change Mode back to Skybox
        RenderSettings.ambientMode = AmbientMode.Skybox;

        // 3. Hide the popup if it's still there
        HidePopup();

        Debug.Log("Factory Power: POWER RESTORED");

        // Force lighting update
        DynamicGI.UpdateEnvironment();
    }

    void HidePopup()
    {
        if (powerOutagePopup != null)
            powerOutagePopup.SetActive(false);
    }
}