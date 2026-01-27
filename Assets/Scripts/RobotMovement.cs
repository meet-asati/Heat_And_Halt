using UnityEngine;
using UnityEngine.InputSystem;

// This line ensures Unity automatically adds a CharacterController if one is missing
[RequireComponent(typeof(CharacterController))]
public class RobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float reverseSpeed = 3f;
    [SerializeField] float turnSpeed = 120f;
    [SerializeField] float gravity = -9.81f; // Added Gravity so robot stays on floor

    [Header("Heat Settings")]
    [SerializeField] float maxHeat = 100f;
    [SerializeField] float walkHeatRate = 5f;
    [SerializeField] float sprintHeatRate = 15f;
    public float CurrentHeat { get; private set; }

    [Header("Animation Settings")]
    [SerializeField] float dampTime = 0.1f;

    private Animator robotAnimator;
    private CharacterController characterController; // Reference to the new component
    private Vector3 verticalVelocity; // To store falling speed

    [Header("Combat Settings")]
    [Tooltip("Time in seconds to lock movement during attack (match your animation length)")]
    public float attackDuration = 1.0f; // Adjust this in Inspector to match your animation
    private bool isAttacking = false;

    [Header("Combat & Heat")]
    public float freezeBeamRange = 20f;
    public LayerMask enemyLayer; // Assign the "Enemy" layer in Inspector
    public Transform firePoint;  // Create an empty GameObject child on the Robot as the "Gun"

    private bool isDead = false;
    public GameObject explosionPrefab;

    void Start()
    {
        robotAnimator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>(); // Get the component
        CurrentHeat = 0f;
    }

    void Update()
    {
        if (isDead) return;
        if (CurrentHeat >= maxHeat)
        {
            Die();
        }
        isAttacking = false;
        if (Keyboard.current == null) return;

        // --- 1. Read Inputs ---
        float moveInput = 0f;
        float turnInput = 0f;

        if (!isAttacking)
        {
            if (Keyboard.current.wKey.isPressed) moveInput += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput -= 1f;

            if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
            if (Keyboard.current.dKey.isPressed) turnInput += 1f;
        }


        if (Keyboard.current.eKey.wasPressedThisFrame && !isAttacking)
        {
            float heatThreshold = maxHeat * 0.5f;

            if (CurrentHeat > heatThreshold)
            {
                // Start the timing sequence routine
                StartCoroutine(PerformAttack());
            }
            else
            {
                Debug.Log("Heat too low to attack!");
            }
        }

        // --- 2. Handle Rotation ---
        if (turnInput != 0)
        {
            // ... existing rotation code ...
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 2. Handle Rotation (Stays the same) ---
        if (turnInput != 0)
        {
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 2. Handle Rotation (Stays the same) ---
        if (turnInput != 0)
        {
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 3. Handle Movement & Heat ---
        bool isSprinting = (moveInput > 0) && Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = 0f;
        float animValue = 0f;

        // Heat Logic
        if (moveInput != 0)
        {
            float heatToAdd = isSprinting ? sprintHeatRate : walkHeatRate;
            CurrentHeat += heatToAdd * Time.deltaTime;
        }
        CurrentHeat = Mathf.Clamp(CurrentHeat, 0, maxHeat);

        // Update UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHeatBar(CurrentHeat, maxHeat);
        }

        // Speed Logic
        if (moveInput > 0)
        {
            if (isSprinting)
            {
                currentSpeed = runSpeed;
                animValue = 1.0f;
            }
            else
            {
                currentSpeed = walkSpeed;
                animValue = 0.5f;
            }
        }
        else if (moveInput < 0)
        {
            currentSpeed = reverseSpeed;
            animValue = -0.5f;
        }

        // --- 4. Apply Movement (UPDATED) ---
        Vector3 move = Vector3.zero;

        if (moveInput != 0)
        {
            // Calculate forward/backward direction relative to robot's rotation
            move = transform.forward * moveInput * currentSpeed;
        }

        // Apply Gravity (CharacterController doesn't have built-in gravity)
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Small force to keep stick to ground
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        // COMBINE movement and gravity
        // Move() handles the collisions automatically!
        characterController.Move((move + verticalVelocity) * Time.deltaTime);


        // --- 5. Update Animator ---
        if (robotAnimator != null)
            robotAnimator.SetFloat("Speed", Mathf.Abs(animValue), dampTime, Time.deltaTime);

    }

    public void ApplyCooling(float coolingAmount)
    {
        CurrentHeat -= coolingAmount;
        CurrentHeat = Mathf.Clamp(CurrentHeat, 0, maxHeat);
    }

    System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;
        robotAnimator.SetTrigger("Fight");

        // Wait for the punch animation to extend (e.g., 0.3s)
        yield return new WaitForSeconds(0.3f);

        // --- LOGIC FIX: Hitbox Adjustment ---
        // define the center of our search (Feet of the robot)
        Vector3 attackCenter = transform.position;

        // Radius: Increased to 3.0f to ensure we reach the ground even if standing tall
        float attackRadius = 10.0f;

        // Detect enemies in the sphere
        Vector3 floorCenter = transform.position + Vector3.down * 0.5f;
        Collider[] hitEnemies = Physics.OverlapSphere(floorCenter, attackRadius, enemyLayer);

        // Debug: See how many enemies we found
        Debug.Log($"Smash! Found {hitEnemies.Length} enemies in range.");

        foreach (Collider enemy in hitEnemies)
        {

            DestroyableObject destObj = enemy.GetComponent<DestroyableObject>();
            if (destObj != null)
            {
                destObj.TakeDamage(1);
            }

            // 1. Check for Drone (Existing)
            DroneAI drone = enemy.GetComponent<DroneAI>();
            if (drone == null) drone = enemy.GetComponentInParent<DroneAI>();

            if (drone != null && drone.IsFrozen)
            {
                drone.SmashDrone();
            }

            // 2. NEW: Check for Boss (Thermal Shock)
            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss != null)
            {
                boss.TakeThermalDamage();
            }
        }
        // -----------------------------------

        yield return new WaitForSeconds(attackDuration - 0.3f);
        isAttacking = false;
    }

    public void IncreaseHeat(float amount)
    {
        CurrentHeat += amount;
        if (CurrentHeat > maxHeat) CurrentHeat = maxHeat;
        Debug.Log($"Heat Increased! Current: {CurrentHeat}");
    }

    // Draw the attack range in the Editor so you can see it
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // This matches the radius in PerformAttack
        Gizmos.DrawWireSphere(transform.position, 3.0f);
    }

    void Die()
    {
        if (isDead) return; // Prevent double dying
        isDead = true;

        Debug.Log("Meltdown Triggered!");

        // 1. Visuals: Spawn Explosion
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 2. Disable the Robot Mesh so it looks like it was destroyed
        // (This finds the visual model inside the robot and hides it)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        // 3. Call the Restart Logic
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerMeltdown();
        }
    }
}