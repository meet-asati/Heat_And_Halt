using UnityEngine;
using System.Collections.Generic;

public class CameraObstructionFader : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Drag the Robot's Transform here")]
    public Transform playerTarget; 
    [Tooltip("Layers that should fade (e.g., Default, Environment). Don't include Player layer!")]
    public LayerMask obstacleLayerMask;

    [Header("Fading Settings")]
    [Range(0f, 1f)]
    public float fadedAlpha = 0.3f; // How transparent it becomes (0 = invisible)
    public float fadeSpeed = 10f;   // How fast it fades in/out

    // Track objects that are currently being manipulated
    private Dictionary<Renderer, FadingObject> runningFaders = new Dictionary<Renderer, FadingObject>();
    
    // Helper class to track state of each object
    private class FadingObject
    {
        public Renderer renderer;
        public Material[] originalMaterials; // Store copies to restore later? 
        // Note: For simple opacity, we just modify the instance color.
        public float currentAlpha;
        public bool isObstructing; // Is the ray currently hitting it?
    }

    void LateUpdate()
    {
        if (playerTarget == null) return;

        // 1. Raycast from Camera to Player
        Vector3 dir = playerTarget.position - transform.position;
        float dist = dir.magnitude;
        
        // RaycastAll ensures we hit EVERYTHING between camera and player
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, dist, obstacleLayerMask);

        // 2. Mark all currently hit objects as "Obstructing"
        HashSet<Renderer> hitsThisFrame = new HashSet<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                hitsThisFrame.Add(rend);
                
                // If this is a new object we haven't seen before, add it to our tracker
                if (!runningFaders.ContainsKey(rend))
                {
                    FadingObject newFader = new FadingObject();
                    newFader.renderer = rend;
                    newFader.currentAlpha = rend.material.color.a; // Start at current alpha
                    newFader.isObstructing = true;
                    runningFaders.Add(rend, newFader);
                }
                else
                {
                    runningFaders[rend].isObstructing = true;
                }
            }
        }

        // 3. Process all tracked objects (Fade In or Fade Out)
        List<Renderer> keysToRemove = new List<Renderer>();

        foreach (var kvp in runningFaders)
        {
            FadingObject fader = kvp.Value;
            Renderer r = fader.renderer;

            // Check if this object was NOT hit this frame
            if (!hitsThisFrame.Contains(r))
            {
                fader.isObstructing = false;
            }

            // Determine target alpha
            float targetAlpha = fader.isObstructing ? fadedAlpha : 1.0f;

            // Smoothly interpolate current alpha
            fader.currentAlpha = Mathf.MoveTowards(fader.currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

            // Apply the color to all materials on this object
            // (Uses _BaseColor for URP or _Color for Standard)
            foreach (Material mat in r.materials)
            {
                Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                color.a = fader.currentAlpha;
                
                if (mat.HasProperty("_BaseColor")) 
                    mat.SetColor("_BaseColor", color);
                else 
                    mat.color = color;
            }

            // Cleanup: If fully opaque and not obstructing, remove from list to save performance
            if (!fader.isObstructing && Mathf.Abs(fader.currentAlpha - 1.0f) < 0.01f)
            {
                keysToRemove.Add(r);
            }
        }

        // Remove restored objects from the dictionary
        foreach (Renderer r in keysToRemove)
        {
            runningFaders.Remove(r);
        }
    }
}
