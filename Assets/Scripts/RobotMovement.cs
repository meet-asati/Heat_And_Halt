using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
    private Vector3 startPosition; // To remember where we started for respawn
    private Quaternion startRotation; // To remember rotation

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

        // Save starting transform for respawn
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        if (isDead) return;

        // 1. Check Heat Death
        if (CurrentHeat >= maxHeat)
        {
            Die();
        }

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
        if (turnInput != 0 && !isAttacking)
        {
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 4. Handle Movement & Heat ---
        bool isSprinting = (moveInput > 0) && Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = 0f;
        float animValue = 0f;

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

        // --- 5. Apply Movement ---
        Vector3 move = Vector3.zero;

        if (moveInput != 0)
        {
            move = transform.forward * moveInput * currentSpeed;
        }

        // Apply Gravity
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        characterController.Move((move + verticalVelocity) * Time.deltaTime);

        // --- 6. Update Animator ---
        if (robotAnimator != null)
        {
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
        isAttacking = true; 
        
        if(robotAnimator != null) robotAnimator.SetFloat("Speed", 0f);

        robotAnimator.SetTrigger("Fight");

        yield return new WaitForSeconds(0.3f);

        Vector3 attackCenter = transform.position;
        float attackRadius = 10.0f; 
        Vector3 floorCenter = transform.position + Vector3.down * 0.5f;
        Collider[] hitEnemies = Physics.OverlapSphere(floorCenter, attackRadius, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            DestroyableObject destObj = enemy.GetComponent<DestroyableObject>();
            if (destObj != null) destObj.TakeDamage(1);

            DroneAI drone = enemy.GetComponent<DroneAI>();
            if (drone == null) drone = enemy.GetComponentInParent<DroneAI>();

            if (drone != null && drone.IsFrozen)
            {
                drone.SmashDrone();
            }

            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss != null)
            {
                boss.TakeThermalDamage();
            }
        }

        yield return new WaitForSeconds(attackDuration - 0.3f);
        
        isAttacking = false; 
    }

    public void IncreaseHeat(float amount)
    {
        CurrentHeat += amount;
        if (CurrentHeat > maxHeat) CurrentHeat = maxHeat;
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

        // Hide the robot visuals
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerMeltdown();
        }

        // --- AUTOMATIC RESPAWN TRIGGER ---
        StartCoroutine(RespawnRoutine(3.0f));
    }

    // Coroutine to handle the wait time
    IEnumerator RespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }

    // Public method to reset the robot
    public void Respawn()
    {
        // 1. Reset Logic
        isDead = false;
        CurrentHeat = 0f;
        isAttacking = false;
        verticalVelocity = Vector3.zero;

        // 2. IMPORTANT: Move CharacterController
        // You MUST disable the controller to teleport it, otherwise it ignores the transform change
        if (characterController != null)
        {
            characterController.enabled = false; 
            transform.position = startPosition;
            transform.rotation = startRotation;
            characterController.enabled = true;
        }
        else
        {
            // Fallback if no controller
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        // 3. Re-enable Visuals
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = true;

        // 4. Reset Animator
        if (robotAnimator != null)
        {
            robotAnimator.Rebind();
            robotAnimator.SetFloat("Speed", 0f);
        }

        // 5. Update UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHeatBar(CurrentHeat, maxHeat);
        }

        Debug.Log("Robot Respawned via Script.");
    }
}