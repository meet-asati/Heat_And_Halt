using UnityEngine;

public class BossVent : MonoBehaviour
{
    public float health = 50f;
    public BossAI bossScript; // Reference to main boss script

    // Called by Drone Laser
    public void TakeLaserDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            // Tell boss we broke the shield
            if(bossScript != null) bossScript.VentDestroyed();
            
            // Visual feedback
            Destroy(gameObject);
        }
    }
}