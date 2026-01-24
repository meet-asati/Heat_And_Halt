using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int enemiesRemaining = 0;
    public bool isGameOver = false;

    [Header("Level References")]
    public GameObject exitDoor; // Drag your Door object here

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Automatically count how many enemies are in the level at the start
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemiesRemaining = enemies.Length;
        Debug.Log($"Level Started. Enemies to defeat: {enemiesRemaining}");
    }

    public void EnemyDefeated()
    {
        enemiesRemaining--;
        Debug.Log($"Enemy Destroyed! Remaining: {enemiesRemaining}");

        if (enemiesRemaining <= 0)
        {
            OpenLevelDoor();
        }
    }

    void OpenLevelDoor()
    {
        Debug.Log("All targets destroyed. DOOR OPENING.");

        if (exitDoor != null)
        {
            // Simple Animation: Rotate the door 90 degrees or Disable it
            // Option A: Destroy it (Simple)
            // Destroy(exitDoor); 

            // Option B: Rotate/Slide (Better)
            exitDoor.SetActive(false); // Make it disappear for now
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("Game Over. Press R to Restart.");
    }

    void Update()
    {
        // Simple Restart Logic
        if (isGameOver && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void TriggerMeltdown()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        Debug.Log("CRITICAL FAILURE! Restarting system in 3 seconds...");

        // Wait 3 seconds so the player sees the explosion
        yield return new WaitForSeconds(3f);

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}