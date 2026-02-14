using UnityEngine;
using UnityEngine.AI; // Required for movement

[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public enum BossState { Idle, Chasing, Attacking, Frozen, VentPhase }

    [Header("State Info")]
    public BossState currentState = BossState.Idle;

    [Header("Stats")]
    public float maxHealth = 1000f;
    private float currentHealth;
    public float heatDamagePerHit = 15f; // Heat added to player per attack

    [Header("Movement")]
    public float attackRange = 3.0f;
    public float rotateSpeed = 5f;
    private NavMeshAgent agent;

    [Header("Thermal Shock (Freeze)")]
    public float freezeDuration = 5.0f;
    private float thawTimer;
    private bool isInvulnerable = false;

    [Header("Phase 2: Vents")]
    public GameObject coolingVentsParent; // Drag the PARENT object holding all vents here
    private int activeVentsCount = 0;
    public GameObject shieldVisuals; // Optional: Force field visual

    [Header("Spawning")]
    public GameObject enemyDronePrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 10f;
    private float spawnTimer;

    [Header("References")]
    public GameObject explosionPrefab;
    public GameObject victoryConsole;
    private RobotMovement playerRobot;
    private Renderer bossRenderer;
    private Color originalColor;
    private Animator animator; // If you have animations

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // We will handle rotation manually for smooth looking

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerRobot = playerObj.GetComponent<RobotMovement>();

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null) originalColor = bossRenderer.material.color;

        // Ensure vents are hidden at start
        if (coolingVentsParent != null) coolingVentsParent.SetActive(false);
        if (victoryConsole != null) victoryConsole.SetActive(false);

        // Update HUD
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowBossHealth(true);
            HUDManager.Instance.UpdateBossHealth(currentHealth, maxHealth);
        }
        animator = GetComponent<Animator>(); // NEW: Get the component
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        HandleMinions();

        switch (currentState)
        {
            case BossState.Idle:
                break;

            case BossState.Chasing:
                MoveToPlayer();
                break;

            case BossState.Attacking:
                AttackLogic();
                break;

            case BossState.Frozen:
                HandleFrozenState();
                break;

            case BossState.VentPhase:
                // NEW: Allow full movement and combat during Vent Phase
                // Check distance to decide if we Chase or Attack
                float dist = Vector3.Distance(transform.position, playerRobot.transform.position);

                if (dist <= attackRange)
                {
                    AttackLogic(); // Allow him to punch you!
                }
                else
                {
                    MoveToPlayer(); // Keep chasing!
                }
                break;
        }
    }

    // --- STATE LOGIC ---

    // Called by your CombatTrigger or Tutorial Manager
    public void WakeUp()
    {
        if (currentState == BossState.Idle)
        {
            currentState = BossState.Chasing;
            if (agent != null) agent.isStopped = false;
            Debug.Log("Boss Activated!");
        }
    }

    void MoveToPlayer()
    {
        if (playerRobot == null || agent == null) return;

        // SAFETY CHECK: Is the agent actually on the NavMesh?
        if (!agent.isOnNavMesh)
        {
            // Try to find the closest valid point
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position); // Teleport to valid ground
            }
            else
            {
                return; // No ground found, abort to prevent crash
            }
        }

        // 1. Move Agent
        agent.SetDestination(playerRobot.transform.position);
        LookAtTarget(playerRobot.transform.position);

        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        // 2. Check Distance
        float dist = Vector3.Distance(transform.position, playerRobot.transform.position);
        if (dist <= attackRange)
        {
            currentState = BossState.Attacking;
        }
    }

    void AttackLogic()
    {
        if (playerRobot == null) return;

        // Stop moving while attacking
        agent.isStopped = true;
        LookAtTarget(playerRobot.transform.position);

        float dist = Vector3.Distance(transform.position, playerRobot.transform.position);

        // If player runs away, go back to chasing
        if (dist > attackRange + 1.0f)
        {
            currentState = BossState.Chasing;
            agent.isStopped = false;
            return;
        }

        if (!IsInvoking("DealDamage"))
        {
            InvokeRepeating("DealDamage", 0.5f, 1.5f);

            // NEW: Trigger the animation
            if (animator != null) animator.SetTrigger("Attack");
        }
    }

    void DealDamage()
    {
        if (currentState != BossState.Attacking)
        {
            CancelInvoke("DealDamage");
            return;
        }

        // Deal Heat Damage to Player
        if (playerRobot != null)
        {
            playerRobot.IncreaseHeat(heatDamagePerHit);
            // Optional: Play punch sound
        }
    }

    void HandleFrozenState()
    {
        thawTimer -= Time.deltaTime;
        if (thawTimer <= 0)
        {
            ThawBoss();
        }
    }

    // --- INTERACTION LOGIC ---

    public void FreezeBoss()
    {
        if (currentState == BossState.Frozen) return;

        currentState = BossState.Frozen;

        // --- MOVEMENT FIX START ---
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;       // Stop calculating path
            agent.velocity = Vector3.zero; // KILL MOMENTUM INSTANTLY
            agent.ResetPath();            // Clear the destination
        }
        // --- MOVEMENT FIX END ---

        CancelInvoke("DealDamage");

        // Visuals
        if (animator != null)
        {
            animator.SetBool("IsFrozen", true);
            animator.SetFloat("Speed", 0f); // Stop walking animation
            animator.ResetTrigger("Attack");
        }

        if (bossRenderer != null) bossRenderer.material.color = Color.cyan;

        thawTimer = freezeDuration;
    }

    public void ThawBoss()
    {

        Debug.Log("Thawing Boss...");

        if (currentState == BossState.VentPhase) return;

        currentState = BossState.Chasing;

        // Safety check before resuming
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (animator != null) animator.SetBool("IsFrozen", false);

        // Reset Visuals
        if (bossRenderer != null) bossRenderer.material.color = originalColor;
    }

    // Called by RobotMovement.cs when player punches
    public void TakeThermalDamage()
    {
        if (isInvulnerable) return;

        if (currentState == BossState.Frozen)
        {
            // 25% Damage Logic
            float damage = maxHealth * 0.25f; // 250 damage
            currentHealth -= damage;

            Debug.Log($"Boss Hit! HP: {currentHealth}");

            if (HUDManager.Instance != null)
                HUDManager.Instance.UpdateBossHealth(currentHealth, maxHealth);

            // Immediately Thaw after taking the massive hit
            ThawBoss();

            // Check for Phase 2 (50%)
            if (currentHealth <= (maxHealth * 0.5f) && coolingVentsParent != null && !coolingVentsParent.activeSelf)
            {
                StartVentPhase();
            }

            if (currentHealth <= 0) Die();
        }
    }

    // --- PHASE 2: VENTS ---

    void StartVentPhase()
    {
        currentState = BossState.VentPhase;
        isInvulnerable = true;

        // REMOVED: agent.isStopped = true;  <-- We want him moving!
        // REMOVED: animator.SetFloat("Speed", 0f); <-- We want him running!

        // 1. Enable Vents
        if (coolingVentsParent != null) coolingVentsParent.SetActive(true);
        activeVentsCount = coolingVentsParent.transform.childCount;

        // 2. Visual Feedback (Red/Shielded)
        if (bossRenderer != null) bossRenderer.material.color = Color.red;

        // 3. Optional: Speed him up for Phase 2?
        if (agent != null) agent.speed += 1.5f; // Makes him faster and scarier

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.ShowTutorial("ARMOR LOCKED! Destroy the moving VENTS!");
    }

    // Called by BossVent.cs
    public void ReportVentDestroyed()
    {
        activeVentsCount--;

        if (activeVentsCount <= 0)
        {
            EndVentPhase();
        }
    }

    void EndVentPhase()
    {
        isInvulnerable = false;
        currentState = BossState.Chasing;

        // Reset speed if you increased it
        if (agent != null) agent.speed -= 1.5f;

        // Visuals
        if (bossRenderer != null) bossRenderer.material.color = originalColor;
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.ShowTutorial("SHIELD DOWN! Freeze and Smash!");
    }

    // --- UTILITIES ---

    void LookAtTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // Don't look up/down
        Quaternion lookRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotateSpeed);
    }

    void HandleMinions()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            if (enemyDronePrefab != null && spawnPoints.Length > 0)
            {
                // Only spawn if less than 5 enemies
                if (GameObject.FindGameObjectsWithTag("Enemy").Length < 5)
                {
                    int randIndex = Random.Range(0, spawnPoints.Length);
                    Instantiate(enemyDronePrefab, spawnPoints[randIndex].position, Quaternion.identity);
                }
            }
            spawnTimer = spawnInterval;
        }
    }

    void Die()
    {
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Victory Logic
        if (victoryConsole != null) victoryConsole.SetActive(true);
        if (HUDManager.Instance != null) HUDManager.Instance.ShowBossHealth(false);
        if (TutorialManager.Instance != null) TutorialManager.Instance.ShowTutorial("TARGET ELIMINATED.\nGo to Control Room.");

        gameObject.SetActive(false);
    }
}