using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Boss Stats")]
    public float maxHealth = 1000f;
    [SerializeField] private float currentHealth; // Serialized so you can see HP in Inspector
    public bool isShielded = false;

    [Header("Thermal Shock Settings")]
    public float freezeDuration = 4.0f; 
    public bool isFrozen = false;
    private float thawTimer;

    [Header("Minion Spawning")]
    public GameObject enemyDronePrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 15f; // DEFAULT to 15
    private float spawnTimer;

    [Header("References")]
    public GameObject coolingVents;
    public GameObject explosionPrefab;
    public GameObject victoryConsole;
    
    // Internal
    private RobotMovement playerRobot;
    private Renderer bossRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        spawnTimer = spawnInterval;
        
        // SAFEGUARD: Prevent crash if Spawn Interval is 0
        if (spawnInterval < 1f) spawnInterval = 15f; 

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerRobot = playerObj.GetComponent<RobotMovement>();

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null) originalColor = bossRenderer.material.color;

        if (victoryConsole != null) victoryConsole.SetActive(false); 
        if (coolingVents != null) coolingVents.SetActive(false);
        
        // SETUP UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowBossHealth(true);
            HUDManager.Instance.UpdateBossHealth(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        // 1. Thawing Logic
        if (isFrozen)
        {
            thawTimer -= Time.deltaTime;
            if (thawTimer <= 0) ThawBoss();
        }

        // 2. Minion Spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnMinions();
            spawnTimer = spawnInterval; // Reset timer
        }

        // 3. Rotation
        if (!isFrozen && playerRobot != null)
        {
             Vector3 lookPos = playerRobot.transform.position;
             lookPos.y = transform.position.y;
             transform.LookAt(lookPos);
        }
    }

    public void FreezeBoss()
    {
        if (isShielded)
        {
            // FEEDBACK: Tell player why it failed
            Debug.Log("BOSS IS SHIELDED! SHOOT THE VENTS!"); 
            return;
        }
        
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
        // 1. Check Shield
        if (isShielded) 
        {
            Debug.Log("ATTACK BLOCKED: Shield is Active! Destroy Vents first!");
            return;
        }

        // 2. Check Frozen Status
        if (!isFrozen)
        {
            Debug.Log("ATTACK BLOCKED: Boss is too hot! Freeze it first!");
            return;
        }

        // 3. Check Robot Heat
        if (playerRobot != null && playerRobot.CurrentHeat > 50f)
        {
            // SUCCESS
            float damage = 250f;
            currentHealth -= damage;
            Debug.Log($"THERMAL SHOCK! Boss HP: {currentHealth}");

            // Update UI
            if (HUDManager.Instance != null)
                HUDManager.Instance.UpdateBossHealth(currentHealth, maxHealth);

            ThawBoss();

            // TRIGGER PHASE 2 (Shields Up)
            if (currentHealth <= 500f && coolingVents != null && !coolingVents.activeSelf)
            {
                ActivateShieldPhase();
            }

            if (currentHealth <= 0) Die();
        }
    }

    void ActivateShieldPhase()
    {
        isShielded = true;
        isFrozen = false; 
        
        // Show Vents
        if (coolingVents != null) coolingVents.SetActive(true); 
        else Debug.LogError("ERROR: Cooling Vents are not assigned in BossAI Inspector!");

        // Visuals
        if (bossRenderer != null) bossRenderer.material.color = Color.red; 
        
        Debug.Log(">>> PHASE 2 STARTED: SHIELDS UP! DESTROY THE VENTS! <<<");
    }

    public void VentDestroyed()
    {
        isShielded = false;
        if (bossRenderer != null) bossRenderer.material.color = originalColor;
        Debug.Log(">>> SHIELD DESTROYED! FREEZE HIM NOW! <<<");
    }

    void SpawnMinions()
    {
        if (enemyDronePrefab == null) return;
        
        // Limit max minions to prevent lag (e.g., max 5 alive at once)
        GameObject[] activeMinions = GameObject.FindGameObjectsWithTag("Enemy");
        // Count only drones, subtract 1 for the Boss itself
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
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        
        gameObject.SetActive(false);
        if (victoryConsole != null) victoryConsole.SetActive(true);
        if (HUDManager.Instance != null) HUDManager.Instance.ShowBossHealth(false);
    }
}