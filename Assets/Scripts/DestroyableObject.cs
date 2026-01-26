using UnityEngine;

public class DestroyableObject : MonoBehaviour
{
    [Header("State")]
    public bool IsFrozen = false;

    [Header("Linked Objects")]
    [Tooltip("Drag the Door GameObject here. It will vanish when this fusebox is smashed.")]
    public GameObject doorToOpen; 

    [Header("Effects")]
    public GameObject rubbleEffect; // Assign explosion particle
    private Renderer objRenderer;
    private Color originalColor;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null) originalColor = objRenderer.material.color;
    }

    // Called by Drone Laser
    public void FreezeObject()
    {
        if (IsFrozen) return;
        
        IsFrozen = true;
        
        // Visual Feedback: Turn Blue
        if (objRenderer != null) objRenderer.material.color = Color.cyan;
        Debug.Log("Fusebox Frozen! Circuits brittle.");
    }

    // Called by Robot Smash
    public void SmashObject()
    {

        Debug.Log($"Attempting to smash: {gameObject.name}");

        if (IsFrozen)
        {
            Debug.Log("Fusebox Smashed!");

            // --- NEW LOGIC START ---
            if (doorToOpen != null)
            {
                DoorController doorLock = doorToOpen.GetComponent<DoorController>();
                if (doorLock != null) doorLock.ReportFuseDestroyed();
                else doorToOpen.SetActive(false);
            }
            // --- NEW LOGIC END ---

            // 2. Play Effects
            if (rubbleEffect != null) Instantiate(rubbleEffect, transform.position, Quaternion.identity);
            
            // 3. Destroy Fusebox
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Fusebox is too tough! Freeze it first.");
        }
    }
}