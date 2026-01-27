using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Content")]
    [TextArea(3, 10)] // Makes a big text box in Inspector
    public string message;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Call the Manager to show the message
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorial(message);
            }

            // Destroy this trigger so the tutorial only appears ONCE
            Destroy(gameObject); 
        }
    }
}