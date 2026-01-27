using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Requirements")]
    public GameObject[] requiredFuseboxes; 

    [Header("Animation")]
    public Animator doorAnimator; 
    private bool isOpen = false;

    void Update()
    {
        if (isOpen) return;
        if (requiredFuseboxes.Length == 0) return;

        if (CheckIfClear())
        {
            OpenDoor();
        }
    }

    bool CheckIfClear()
    {
        // Loop through the list to see if anything is keeping the door locked
        for (int i = 0; i < requiredFuseboxes.Length; i++)
        {
            GameObject obj = requiredFuseboxes[i];

            // 1. If this slot is empty (Object Destroyed), we just continue checking the others.
            if (obj == null) 
            {
                // Proceed to next item
                continue; 
            }

            // 2. If object exists, check its tag
            if (obj.CompareTag("Fusebox"))
            {
                // We found a living fusebox. Door stays closed.
                return false; 
            }
            else
            {
                // We found an object, but it has the WRONG TAG.
                Debug.LogWarning($"Ignoring object '{obj.name}' because it is not tagged 'Fusebox'.");
            }
        }

        // If we get here, it means we found NO valid living fuseboxes.
        return true; 
    }

    void OpenDoor()
    {
        Debug.Log("DOOR OPENING! Cause: All required Fuseboxes are null/destroyed.");
        isOpen = true;
        
        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");
        else
            gameObject.SetActive(false); 
    }
}