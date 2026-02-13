using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Content")]
    [TextArea(3, 10)] 
    public string message;

    [Header("Optional: Boss Trigger")]
    [Tooltip("Drag the BossAI object here if this tutorial starts a fight.")]
    public BossAI bossToWake; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (TutorialManager.Instance != null)
            {
                // We pass a "Lambda Function" () => { ... } as the second argument
                TutorialManager.Instance.ShowTutorial(message, () => 
                {
                    // This code runs AFTER the user presses Enter
                    if (bossToWake != null) 
                    {
                        bossToWake.WakeUp();
                    }
                });
            }

            Destroy(gameObject); 
        }
    }
}