using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Settings")]
    public int fusesNeeded = 2; // How many must be smashed?

    private int fusesDestroyed = 0;

    public void ReportFuseDestroyed()
    {
        fusesDestroyed++;
        Debug.Log($"Security Update: {fusesDestroyed}/{fusesNeeded} Fuseboxes Destroyed.");

        if (fusesDestroyed >= fusesNeeded)
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        Debug.Log("Access Granted. Opening Door.");
        gameObject.SetActive(false); // Or play an animation here
    }
}