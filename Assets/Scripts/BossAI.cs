using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 1000f;
    private float currentHealth;
    public bool isShielded = false;

    [Header("Thermal Shock Settings")]
    public float freezeDuration = 4.0f;
    public bool isFrozen = false;
    private float thawTimer;

    [Header("Minion Spawning")]
    public GameObject enemyDronePrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 15f; 
    private float spawnTimer;

    [Header("References")]
    public GameObject coolingVents;
    public GameObject explosionPrefab;
    public GameObject victoryConsole;

    // Internal
    private RobotMovement playerRobot;
    private Renderer bossRenderer;
    private Color originalColor;

    [Header("Tutorial / Messages")]
    [TextArea]
    public string phaseTwoMessage = "ARMOR CRITICAL! \n\nVENTS EXPOSED on Shoulders. \nUse Drone Laser [RMB] to destroy them!";
    private bool hasTriggeredPhase2 = false;

    [TextArea] // <--- NEW VARIABLE
    public string victoryMessage = "TARGET ELIMINATED.\n\nThreat Neutralized. \nPROCEED to the Control Room behind the door to execute sabotage prevention protocols.";

    void Start()
    {
        currentHealth = maxHealth;
        spawnTimer = spawnInterval;

        if (spawnInterval < 1f) spawnInterval = 15f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerRobot = playerObj.GetComponent<RobotMovement>();

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null) originalColor = bossRenderer.material.color;

        if (victoryConsole != null) victoryConsole.SetActive(false);
        if (coolingVents != null) coolingVents.SetActive(false);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowBossHealth(true);
            HUDManager.Instance.UpdateBossHealth(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        // Thawing Logic
        if (isFrozen)
        {
            thawTimer -= Time.deltaTime;
            if (thawTimer <= 0) ThawBoss();
        }

        // Minion Spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnMinions();
            spawnTimer = spawnInterval; 
        }

        // Rotation
        if (!isFrozen && playerRobot != null)
        {
            Vector3 lookPos = playerRobot.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }
    }

    public void FreezeBoss()
    {
        if (isShielded) return;
        if (isFrozen) return;

        isFrozen = true;
        thawTimer = freezeDuration;
        if (bossRenderer != null) bossRenderer.material.color = Color.cyan;
    }

    void ThawBoss()
    {
        isFrozen = false;
        if (bossRenderer != null)
            bossRenderer.material.color = isShielded ? Color.red : originalColor;
    }

    public void TakeThermalDamage()
    {
        if (isShielded) return;
        if (!isFrozen) return;

        if (playerRobot != null && playerRobot.CurrentHeat > 50f)
        {
            float damage = 250f;
            currentHealth -= damage;
            
            if (HUDManager.Instance != null)
                HUDManager.Instance.UpdateBossHealth(currentHealth, maxHealth);

            ThawBoss();

            // Check for Phase 2 (50%)
            if (currentHealth <= (maxHealth * 0.5f) && !isShielded)
            {
                ActivateShieldPhase();
            }

            if (currentHealth <= 0) Die();
        }
    }

    void ActivateShieldPhase()
    {
        if (hasTriggeredPhase2) return;

        isShielded = true;
        isFrozen = false;
        hasTriggeredPhase2 = true;

        if (coolingVents != null) coolingVents.SetActive(true);
        if (bossRenderer != null) bossRenderer.material.color = Color.red;

        // Show Phase 2 Message
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowTutorial(phaseTwoMessage);
        }
    }

    public void VentDestroyed()
    {
        isShielded = false;
        if (bossRenderer != null) bossRenderer.material.color = originalColor;
    }

    void SpawnMinions()
    {
        if (enemyDronePrefab == null) return;
        GameObject[] activeMinions = GameObject.FindGameObjectsWithTag("Enemy");
        if (activeMinions.Length > 6) return;

        foreach (Transform sp in spawnPoints)
        {
            if (sp != null)
                Instantiate(enemyDronePrefab, sp.position, Quaternion.identity);
        }
    }

    void Die()
    {
        Debug.Log("BOSS DEFEATED.");
        
        // 1. Explosion Visuals
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // 2. Hide Boss Model
        gameObject.SetActive(false);

        // 3. Unlock Next Area (Optional: Show Console)
        if (victoryConsole != null) victoryConsole.SetActive(true);

        // 4. Hide Boss Bar
        if (HUDManager.Instance != null) HUDManager.Instance.ShowBossHealth(false);

        // 5. --- SHOW VICTORY MESSAGE ---
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowTutorial(victoryMessage);
        }
    }
}