using UnityEngine;

public class CombatTrigger : MonoBehaviour
{
    [Header("Enemies to Wake Up")]
    public DroneAI[] drones; // Drag all ambush drones here
    
    [Header("Optional Boss")]
    public BossAI boss; // Drag boss here if this is the boss room

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            ActivateEnemies();
        }
    }

    void ActivateEnemies()
    {
        Debug.Log("Ambush Triggered!");

        // Wake up all assigned drones
        foreach (DroneAI drone in drones)
        {
            if (drone != null) drone.WakeUp();
        }

        // Wake up boss (if assigned)
        if (boss != null)
        {
            // We need to add a similar WakeUp method to BossAI if you haven't yet
            boss.enabled = true; // Simple way to wake boss
        }

        // Destroy the trigger so it doesn't happen again
        Destroy(gameObject);
    }
}