using UnityEngine;
using UnityEngine.Events;

public class DestroyableObject : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3; 
    private int currentHealth;

    [Header("Visuals (Optional)")]
    public GameObject normalModel;
    public GameObject frozenModel;

    [Header("Progression Gate")]
    [Tooltip("If true, this object cannot be destroyed until this flag is set to false via script.")]
    public bool isIndestructible = false;
    [Tooltip("Message to display if player tries to destroy it while locked.")]
    public string lockedMessage = "Power must be restored first!";
    // Reference to a UI manager or simple text to show the warning
    public GameObject lockedMessageUI; 

    [Header("Events")]
    public UnityEvent OnDestroyed; 

    private bool isFrozen = false;
    private Renderer myRenderer; 

    void Start()
    {
        currentHealth = maxHealth;
        myRenderer = GetComponent<Renderer>(); 
        
        if (normalModel != null) normalModel.SetActive(true);
        if (frozenModel != null) frozenModel.SetActive(false);
        if (lockedMessageUI != null) lockedMessageUI.SetActive(false);
    }

    // Call this to UNLOCK the object (e.g., when power is restored)
    public void SetDestructible(bool canDestroy)
    {
        isIndestructible = !canDestroy;
    }

    public void Freeze()
    {
        if (isFrozen) return;

        isFrozen = true;

        if (normalModel != null && frozenModel != null)
        {
            normalModel.SetActive(false);
            frozenModel.SetActive(true);
        }
        else if (myRenderer != null)
        {
            myRenderer.material.color = Color.cyan; 
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isFrozen) return;

        // NEW LOGIC: Check if we are allowed to destroy this yet
        if (isIndestructible)
        {
            Debug.Log(lockedMessage);
            ShowLockedMessage();
            return; // Exit the function, taking NO damage
        }

        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    void ShowLockedMessage()
    {
        if (lockedMessageUI != null)
        {
            lockedMessageUI.SetActive(true);
            Invoke("HideLockedMessage", 3f); // Hide after 3 seconds
        }
    }

    void HideLockedMessage()
    {
        if (lockedMessageUI != null) lockedMessageUI.SetActive(false);
    }

    void Die()
    {
        OnDestroyed.Invoke(); 
        Destroy(gameObject);
    }
}