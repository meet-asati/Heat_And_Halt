using UnityEngine;
using UnityEngine.Events;
using System.Collections; // Required for Coroutines

[RequireComponent(typeof(AudioSource))]
public class DestroyableObject : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 2;
    private int currentHealth;

    [Header("Visuals")]
    public GameObject normalModel;
    public GameObject frozenModel;
    
    [Header("Freeze Settings")]
    public float freezeDuration = 5.0f; // Time in seconds for the color to change
    public Color frozenColor = Color.cyan; // The target color when frozen

    [Header("Destruction Settings")]
    [Tooltip("Number of debris pieces to spawn.")]
    public int debrisPieces = 8;
    [Tooltip("Explosion force of the debris.")]
    public float explosionForce = 500f;
    [Tooltip("Radius of the explosion.")]
    public float explosionRadius = 2f;

    [Header("Progression Gate")]
    public bool isIndestructible = false;
    public string lockedMessage = "Power must be restored first!";
    public GameObject lockedMessageUI;

    [Header("Audio Settings")]
    public AudioClip hitSound;
    public AudioClip destroySound;

    [Header("Events")]
    public UnityEvent OnDestroyed;

    private bool isFrozen = false;
    private Renderer myRenderer;
    private AudioSource audioSource;
    private Color originalColor; // Store the starting color

    void Start()
    {
        currentHealth = maxHealth;
        myRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        // Cache the original color if a renderer exists
        if (myRenderer != null)
        {
            originalColor = myRenderer.material.color;
        }

        if (normalModel != null) normalModel.SetActive(true);
        if (frozenModel != null) frozenModel.SetActive(false);
        if (lockedMessageUI != null) lockedMessageUI.SetActive(false);
    }

    public void SetDestructible(bool canDestroy)
    {
        isIndestructible = !canDestroy;
    }

    public void Freeze()
    {
        if (isFrozen) return;

        // Start the gradual freeze routine instead of instant snapping
        StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        isFrozen = true;
        float elapsed = 0f;

        // PATH A: If using a single renderer, fade the color
        if (myRenderer != null)
        {
            while (elapsed < freezeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / freezeDuration;

                // Smoothly interpolate from current color to frozen color
                myRenderer.material.color = Color.Lerp(originalColor, frozenColor, t);

                yield return null; // Wait for the next frame
            }
            // Ensure we land exactly on the target color
            myRenderer.material.color = frozenColor;
        }
        
        // PATH B: If using Model Swapping (Optional support)
        // We do this AFTER the duration so the fade (if any) happens first
        if (normalModel != null && frozenModel != null)
        {
            normalModel.SetActive(false);
            frozenModel.SetActive(true);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!isFrozen) return;

        if (isIndestructible)
        {
            Debug.Log(lockedMessage);
            ShowLockedMessage();
            return;
        }

        currentHealth -= damage;

        if (currentHealth > 0 && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (currentHealth <= 0) Die();
    }

    void ShowLockedMessage()
    {
        if (lockedMessageUI != null)
        {
            lockedMessageUI.SetActive(true);
            Invoke("HideLockedMessage", 3f);
        }
    }

    void HideLockedMessage()
    {
        if (lockedMessageUI != null) lockedMessageUI.SetActive(false);
    }

    void Die()
    {
        // 1. Play Destroy Audio (Your custom volume logic preserved)
        AudioClip clipToPlay = destroySound != null ? destroySound : hitSound;
        if (clipToPlay != null)
        {
            GameObject tempAudio = new GameObject("TempAudio");
            tempAudio.transform.position = transform.position;
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = clipToPlay;
            tempSource.volume = 2.0f; 
            tempSource.spatialBlend = 1.0f;
            tempSource.minDistance = 5f;
            tempSource.Play();
            Destroy(tempAudio, clipToPlay.length);
        }

        OnDestroyed.Invoke();

        // 2. Spawn the Debris (The "Programmer Art" approach)
        SpawnProceduralDebris();

        // 3. Destroy the main object
        Destroy(gameObject);
    }

    void SpawnProceduralDebris()
    {
        // Create a temporary parent container for organization
        GameObject debrisHolder = new GameObject("Debris_" + gameObject.name);
        debrisHolder.transform.position = transform.position;

        Vector3 originalScale = transform.localScale;

        for (int i = 0; i < debrisPieces; i++)
        {
            // Create a primitive cube
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Randomly position it inside the object's volume
            piece.transform.position = transform.position + Random.insideUnitSphere * 0.5f;
            piece.transform.rotation = Random.rotation;
            
            // Scale it down (roughly 1/3rd size of the original)
            piece.transform.localScale = originalScale * 0.05f;
            
            // Match the color (It will now be the Frozen Color!)
            if (myRenderer != null)
            {
                piece.GetComponent<Renderer>().material.color = myRenderer.material.color;
            }

            // Add Physics
            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            
            // Apply Explosion Force
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            // Parent to holder
            piece.transform.parent = debrisHolder.transform;
        }

        // Clean up debris after 4 seconds
        Destroy(debrisHolder, 4f);
    }
}