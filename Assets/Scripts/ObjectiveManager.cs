using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;
    public TextMeshProUGUI objectiveTextUI;

    [Header("Objectives List")]
    // We store these here so we can reference them easily via dropdowns/events if needed, 
    // or just pass raw strings.
    public string startObj = "Move Forward";
    public string destroyObj = "Destroy objects and fusebox";
    public string restoreObj = "Restore Electricity";
    public string exitObj = "Proceed to exit door";
    public string droneObj = "Destroy the enemy drone";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 1. Set the initial objective on Start
        UpdateObjective(startObj);
    }

    // Call this function from ANY trigger or event
    public void UpdateObjective(string newObjective)
    {
        if (objectiveTextUI != null)
        {
            objectiveTextUI.text = newObjective;
            
            // Optional: Play a sound or flash animation here
            Debug.Log("Objective Updated: " + newObjective);
        }
    }
}