using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // Ensures the object has an AudioSource
public class DoorSlide : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How high the door should slide.")]
    public float slideHeight = 3f; 
    
    [Tooltip("How fast the door moves.")]
    public float openSpeed = 2.0f;

    [Header("Audio Settings")]
    public AudioClip openSound; // Drag your door sound here

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isOpen = false;
    private AudioSource audioSource;

    void Start()
    {
        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Remember where the door started
        initialPosition = transform.position;
        // Calculate where it should end up (Current Y + Slide Height)
        targetPosition = initialPosition + (Vector3.up * slideHeight);
    }

    // Call this function from your Fuse Box's OnDestroyed event
    public void OpenDoor()
    {
        if (!isOpen)
        {
            // Play the sound immediately when opening starts
            if (audioSource != null && openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            StartCoroutine(SlideRoutine());
        }
    }

    private IEnumerator SlideRoutine()
    {
        isOpen = true;
        
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // Smoothly move towards the target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        // Ensure it snaps exactly to the final position
        transform.position = targetPosition;
    }
}