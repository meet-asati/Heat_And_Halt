using UnityEngine;

public class DestroyableObject : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3; 
    private int currentHealth;

    [Header("Visuals (Optional)")]
    public GameObject normalModel;
    public GameObject frozenModel;

    private bool isFrozen = false;
    private Renderer myRenderer; // To change color

    void Start()
    {
        currentHealth = maxHealth;
        myRenderer = GetComponent<Renderer>(); // Get the renderer automatically
        
        // Safety: If normal model is not assigned, assume this object is the model
        if (normalModel != null) normalModel.SetActive(true);
        if (frozenModel != null) frozenModel.SetActive(false);
    }

    public void Freeze()
    {
        if (isFrozen) return;

        isFrozen = true;
        Debug.Log("TARGET FROZEN!");

        // Option A: Swap Models (If you have them)
        if (normalModel != null && frozenModel != null)
        {
            normalModel.SetActive(false);
            frozenModel.SetActive(true);
        }
        // Option B: Change Color (If you don't have models)
        else if (myRenderer != null)
        {
            myRenderer.material.color = Color.cyan; // Turn it Blue/Cyan
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isFrozen)
        {
            // Optional: Play "Clang" sound
            return; 
        }

        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}