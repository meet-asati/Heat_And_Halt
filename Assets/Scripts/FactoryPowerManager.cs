using UnityEngine;
using UnityEngine.Rendering;

public class FactoryPowerManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject powerOutagePopup; 

    [Header("Lighting Settings")]
    public Color blackoutColor = new Color32(60, 60, 60, 255); 
    private Material defaultSkybox;

    [Header("Progression")]
    [Tooltip("Drag ALL fuse boxes that should be locked/unlocked here.")]
    // specific change: changed from single variable to an Array []
    public DestroyableObject[] progressionFuseBoxes; 

    void Start()
    {
        defaultSkybox = RenderSettings.skybox;
        if (powerOutagePopup != null) powerOutagePopup.SetActive(false);

        // Lock ALL progression boxes at start (Optional safety check)
        LockAllFuseBoxes();
    }

    public void TriggerBlackout()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = blackoutColor;

        if (powerOutagePopup != null)
        {
            powerOutagePopup.SetActive(true);
            Invoke("HidePopup", 5f); 
        }

        // Lock them just in case
        LockAllFuseBoxes();

        DynamicGI.UpdateEnvironment();
    }

    public void RestorePower()
    {
        RenderSettings.skybox = defaultSkybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        
        if (powerOutagePopup != null) powerOutagePopup.SetActive(false);

        // UNLOCK ALL FUSE BOXES
        UnlockAllFuseBoxes();

        DynamicGI.UpdateEnvironment();
    }

    // Helper function to lock everything in the list
    void LockAllFuseBoxes()
    {
        foreach (DestroyableObject box in progressionFuseBoxes)
        {
            if (box != null) box.SetDestructible(false);
        }
    }

    // Helper function to unlock everything in the list
    void UnlockAllFuseBoxes()
    {
        foreach (DestroyableObject box in progressionFuseBoxes)
        {
            if (box != null) 
            {
                box.SetDestructible(true);
            }
        }
        Debug.Log("Factory Power Restored! Progression Fuse Boxes Unlocked.");
    }

    void HidePopup()
    {
        if (powerOutagePopup != null) powerOutagePopup.SetActive(false);
    }
}