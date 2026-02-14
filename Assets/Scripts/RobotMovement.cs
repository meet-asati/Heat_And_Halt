using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float reverseSpeed = 3f;
    [SerializeField] float turnSpeed = 120f;
    [SerializeField] float gravity = -9.81f;

    [Header("Heat Settings")]
    [SerializeField] float maxHeat = 100f;
    [SerializeField] float walkHeatRate = 5f;
    [SerializeField] float sprintHeatRate = 15f;
    public float CurrentHeat { get; private set; }

    [Header("Animation Settings")]
    [SerializeField] float dampTime = 0.1f;

    private Animator robotAnimator;
    private CharacterController characterController;
    private Vector3 verticalVelocity;

    [Header("Combat Settings")]
    [Tooltip("Time in seconds to lock movement during attack (match your animation length)")]
    public float attackDuration = 1.0f;
    private bool isAttacking = false;

    [Header("Combat & Heat")]
    public float freezeBeamRange = 20f;
    public LayerMask enemyLayer;
    public Transform firePoint;

    private bool isDead = false;
    public GameObject explosionPrefab;

    void Start()
    {
        robotAnimator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        CurrentHeat = 0f;
    }

    void Update()
    {
        if (isDead) return;

        // 1. Check Heat Death
        if (CurrentHeat >= maxHeat)
        {
            Die();
        }

        // --- FIX: Removed "isAttacking = false;" here. This was the bug causing movement while fighting. ---

        if (Keyboard.current == null) return;

        // --- 2. Read Inputs ---
        float moveInput = 0f;
        float turnInput = 0f;

        // ONLY read movement/attack inputs if we are NOT currently attacking
        if (!isAttacking)
        {
            if (Keyboard.current.wKey.isPressed) moveInput += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput -= 1f;

            if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
            if (Keyboard.current.dKey.isPressed) turnInput += 1f;

            // Attack Input (Only allowed if not already attacking)
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                float heatThreshold = maxHeat * 0.5f;

                if (CurrentHeat > heatThreshold)
                {
                    StartCoroutine(PerformAttack());
                }
                else
                {
                    Debug.Log("Heat too low to attack!");
                }
            }
        }

        // --- 3. Handle Rotation ---
        // We allow rotation only if moving or explicitly turning (optional: you can disable this during attack too if you want)
        if (turnInput != 0 && !isAttacking)
        {
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 4. Handle Movement & Heat ---
        bool isSprinting = (moveInput > 0) && Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = 0f;
        float animValue = 0f;

        // Force inputs to zero if attacking (Safety Check)
        if (isAttacking)
        {
            moveInput = 0f;
            isSprinting = false;
        }

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
        // If moveInput is 0 (stopped or attacking), animValue stays 0

        // --- 5. Apply Movement ---
        Vector3 move = Vector3.zero;

        if (moveInput != 0)
        {
            move = transform.forward * moveInput * currentSpeed;
        }

        // Apply Gravity (Always apply gravity, even when attacking, so you don't float)
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        characterController.Move((move + verticalVelocity) * Time.deltaTime);

        // --- 6. Update Animator ---
        if (robotAnimator != null)
        {
            // If attacking, force the speed parameter to 0 immediately so legs stop moving
            if (isAttacking)
            {
                robotAnimator.SetFloat("Speed", 0f);
            }
            else
            {
                robotAnimator.SetFloat("Speed", Mathf.Abs(animValue), dampTime, Time.deltaTime);
            }
        }
    }

    public void ApplyCooling(float coolingAmount)
    {
        CurrentHeat -= coolingAmount;
        CurrentHeat = Mathf.Clamp(CurrentHeat, 0, maxHeat);
    }

    System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true; // Lock movement
        
        // VISUAL FIX: Immediately stop the walking animation
        if(robotAnimator != null) robotAnimator.SetFloat("Speed", 0f);

        robotAnimator.SetTrigger("Fight");

        // Wait for the impact point of the animation (adjust 0.3f to match your specific punch)
        yield return new WaitForSeconds(0.3f);

        // --- Hitbox Logic ---
        Vector3 attackCenter = transform.position;
        float attackRadius = 10.0f; 

        // Lower the center slightly to hit small enemies on the floor
        Vector3 floorCenter = transform.position + Vector3.down * 0.5f;
        Collider[] hitEnemies = Physics.OverlapSphere(floorCenter, attackRadius, enemyLayer);

        Debug.Log($"Smash! Found {hitEnemies.Length} enemies in range.");

        foreach (Collider enemy in hitEnemies)
        {
            // 1. Generic Destroyable Objects
            DestroyableObject destObj = enemy.GetComponent<DestroyableObject>();
            if (destObj != null) destObj.TakeDamage(1);

            // 2. Drones
            DroneAI drone = enemy.GetComponent<DroneAI>();
            if (drone == null) drone = enemy.GetComponentInParent<DroneAI>();

            if (drone != null && drone.IsFrozen)
            {
                drone.SmashDrone();
            }

            // 3. Boss
            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss != null)
            {
                boss.TakeThermalDamage();
            }
        }

        // Wait for the rest of the animation to finish
        yield return new WaitForSeconds(attackDuration - 0.3f);
        
        isAttacking = false; // Unlock movement
    }

    public void IncreaseHeat(float amount)
    {
        CurrentHeat += amount;
        if (CurrentHeat > maxHeat) CurrentHeat = maxHeat;
        Debug.Log($"Heat Increased! Current: {CurrentHeat}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3.0f);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Meltdown Triggered!");

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerMeltdown();
        }
    }
}