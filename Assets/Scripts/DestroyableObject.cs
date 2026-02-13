using UnityEngine;
using UnityEngine.Events; // Added to support UnityEvents

public class DestroyableObject : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3; 
    private int currentHealth;

    [Header("Visuals (Optional)")]
    public GameObject normalModel;
    public GameObject frozenModel;

    [Header("Events")]
    // This allows us to trigger the lighting change from the Inspector
    public UnityEvent OnDestroyed; 

    private bool isFrozen = false;
    private Renderer myRenderer; 

    void Start()
    {
        currentHealth = maxHealth;
        myRenderer = GetComponent<Renderer>(); 
        
        if (normalModel != null) normalModel.SetActive(true);
        if (frozenModel != null) frozenModel.SetActive(false);
    }

    public void Freeze()
    {
        if (isFrozen) return;

        isFrozen = true;
        // Debug.Log("TARGET FROZEN!"); // Commented out to reduce console spam

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
        if (!isFrozen)
        {
            // Optional: Play "Clang" sound here
            return; 
        }

        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        // trigger whatever logic is connected to this object (e.g., Lights Off)
        OnDestroyed.Invoke(); 

        Destroy(gameObject);
    }
}