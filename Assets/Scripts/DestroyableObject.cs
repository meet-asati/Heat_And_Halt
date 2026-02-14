using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))] // Ensures the object has an AudioSource
public class DestroyableObject : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 2; 
    private int currentHealth;

    [Header("Visuals (Optional)")]
    public GameObject normalModel;
    public GameObject frozenModel;

    [Header("Progression Gate")]
    [Tooltip("If true, this object cannot be destroyed until this flag is set to false via script.")]
    public bool isIndestructible = false;
    [Tooltip("Message to display if player tries to destroy it while locked.")]
    public string lockedMessage = "Power must be restored first!";
    public GameObject lockedMessageUI; 

    [Header("Audio Settings")]
    [Tooltip("Sound to play every time the object is hit but not destroyed.")]
    public AudioClip hitSound;
    [Tooltip("Sound to play when the object breaks completely.")]
    public AudioClip destroySound;

    [Header("Events")]
    public UnityEvent OnDestroyed; 

    private bool isFrozen = false;
    private Renderer myRenderer; 
    private AudioSource audioSource; // Reference to the AudioSource

    void Start()
    {
        currentHealth = maxHealth;
        myRenderer = GetComponent<Renderer>(); 
        audioSource = GetComponent<AudioSource>(); // Get the component
        
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
        // 1. Only take damage if frozen
        if (!isFrozen) return;

        // 2. Check if locked
        if (isIndestructible)
        {
            Debug.Log(lockedMessage);
            ShowLockedMessage();
            return; 
        }

        currentHealth -= damage;

        // 3. Play Hit Audio
        // If we are still alive, play the hit sound on this object
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
        // 1. Play Destroy Audio with Custom Volume
        AudioClip clipToPlay = destroySound != null ? destroySound : hitSound;

        if (clipToPlay != null)
        {
            // Create a temporary game object to play the sound
            GameObject tempAudio = new GameObject("TempAudio");
            tempAudio.transform.position = transform.position;

            // Add an AudioSource to it
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = clipToPlay;
            tempSource.volume = 2.0f; // Force volume to max
            tempSource.spatialBlend = 1.0f; // Make it 3D
            tempSource.minDistance = 5f; // Ensures it's loud enough close up
            
            tempSource.Play();

            // Destroy the temporary object after the clip finishes
            Destroy(tempAudio, clipToPlay.length);
        }

        OnDestroyed.Invoke(); 
        Destroy(gameObject);
    }
}