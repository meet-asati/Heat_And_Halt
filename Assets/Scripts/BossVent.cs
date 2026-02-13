using UnityEngine;

public class BossVent : MonoBehaviour
{
    public float health = 50f;
    // Drag the main BOSS object here in the inspector
    public BossAI bossScript; 

    public void TakeLaserDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            // Report to the boss before dying
            if(bossScript != null) 
            {
                bossScript.ReportVentDestroyed();
            }
            
            // Visual FX could go here
            Destroy(gameObject);
        }
    }
}