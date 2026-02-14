using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    [Header("Arrow Groups (Drag Parents Here)")]
    public GameObject pathToObstacles;
    public GameObject pathToFuse;
    public GameObject pathToExit;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Start state: Show only path to obstacles
        ShowPath(pathToObstacles);
    }

    // Helper function to turn one on and others off
    public void ShowPath(GameObject activePath)
    {
        if (pathToObstacles != null) pathToObstacles.SetActive(false);
        if (pathToFuse != null) pathToFuse.SetActive(false);
        if (pathToExit != null) pathToExit.SetActive(false);

        if (activePath != null) activePath.SetActive(true);
    }

    // Call these from your Mission/Level scripts
    public void ActivatePathToFuse()
    {
        ShowPath(pathToFuse);
    }

    public void ActivatePathToExit()
    {
        ShowPath(pathToExit);
    }
}