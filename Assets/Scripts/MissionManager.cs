using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Setup")]
    [Tooltip("Drag the things the player must destroy or reach here, IN ORDER.")]
    public List<GameObject> objectives; 

    [Header("Navigation Arrows")]
    [Tooltip("Drag the parent Arrow Objects here. Element 0 leads to Objective 0, etc.")]
    public List<GameObject> arrowPaths;

    // [Header("Level Settings")]
    // [TextArea] public string completeMessage = "LEVEL COMPLETE. Proceed to Exit.";
    
    // Internal State
    private int currentIndex = 0;

    void Start()
    {
        // 1. Hide ALL paths initially to avoid clutter
        foreach (var path in arrowPaths)
        {
            if (path != null) path.SetActive(false);
        }

        // 2. Show only the first path (if it exists)
        if (arrowPaths.Count > 0 && arrowPaths[0] != null)
        {
            arrowPaths[0].SetActive(true);
        }
        
        // 3. Show Tutorial for the first objective
        // ShowCurrentTutorial();
    }

    void Update()
    {
        // If we finished everything, stop checking
        if (currentIndex >= objectives.Count) return;

        // LOGIC: Check if the current target is "Gone"
        // "Gone" means Destroyed (null) OR Disabled (SetActive false)
        GameObject currentTarget = objectives[currentIndex];

        if (currentTarget == null || !currentTarget.activeInHierarchy)
        {
            // Target is done! Move to next.
            AdvanceMission();
        }
    }

    void AdvanceMission()
    {
        // 1. Hide the Old Path
        if (currentIndex < arrowPaths.Count && arrowPaths[currentIndex] != null)
        {
            arrowPaths[currentIndex].SetActive(false);
        }

        // 2. Increment Step
        currentIndex++;

        // // 3. Check if Level is Done
        // if (currentIndex >= objectives.Count)
        // {
        //     Debug.Log("Mission Complete!");
        //     if (TutorialManager.Instance != null)
        //         TutorialManager.Instance.ShowTutorial(completeMessage);
        //     return;
        // }

        // 4. Show the New Path
        if (currentIndex < arrowPaths.Count && arrowPaths[currentIndex] != null)
        {
            arrowPaths[currentIndex].SetActive(true);
        }
        
        // 5. Show Tutorial (Optional)
        // ShowCurrentTutorial();
    }
    
    void ShowCurrentTutorial()
    {
        if (TutorialManager.Instance == null) return;
        
        // You can customize this logic or add a "List<string> tutorialMessages" to match objectives
        // For now, we just log it
        Debug.Log($"Objective {currentIndex} Started.");
    }
}