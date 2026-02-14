using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Content")]
    [TextArea(3, 10)] 
    public string message;

    [Header("Objective Update")]
    public bool updatesObjective = false;
    public string newObjectiveText;

    [Header("Optional: Boss Trigger")]
    public BossAI bossToWake; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorial(message, () => 
                {
                    // Triggers AFTER 'Enter' is pressed
                    if (bossToWake != null) bossToWake.WakeUp();
                    
                    // NEW: Update Objective
                    if (updatesObjective && ObjectiveManager.Instance != null)
                    {
                        ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
                    }
                });
            }
            Destroy(gameObject); 
        }
    }
}