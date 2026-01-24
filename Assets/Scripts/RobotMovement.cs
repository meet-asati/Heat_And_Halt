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

    void Start()
    {
        robotAnimator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>(); // Get the component
        CurrentHeat = 0f;
    }

    void Update()
    {
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

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            FireFreezeBeam();
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
        robotAnimator.SetTrigger("Fight");

        // Wait for the punch animation to extend (e.g., 0.3s)
        yield return new WaitForSeconds(0.3f);

        // --- LOGIC FIX: Hitbox Adjustment ---
        // define the center of our search (Feet of the robot)
        Vector3 attackCenter = transform.position;

        // Radius: Increased to 3.0f to ensure we reach the ground even if standing tall
        float attackRadius = 3.0f;

        // Detect enemies in the sphere
        Collider[] hitEnemies = Physics.OverlapSphere(attackCenter, attackRadius, enemyLayer);

        // Debug: See how many enemies we found
        Debug.Log($"Smash! Found {hitEnemies.Length} enemies in range.");

        foreach (Collider enemy in hitEnemies)
        {
            // Try to get the Drone script from the object or its parent
            DroneAI drone = enemy.GetComponent<DroneAI>();
            if (drone == null) drone = enemy.GetComponentInParent<DroneAI>();

            // Check if we found a drone AND it is frozen
            if (drone != null)
            {
                if (drone.IsFrozen)
                {
                    drone.SmashDrone();
                    Debug.Log("Confirmed: Frozen Drone Destroyed.");
                }
                else
                {
                    Debug.Log("Hit a drone, but it wasn't frozen yet!");
                }
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

    void FireFreezeBeam()
    {
        // Debug: Draw a red line in the Scene view to show where you are shooting
        Debug.DrawRay(firePoint.position, firePoint.forward * freezeBeamRange, Color.red, 1.0f);

        RaycastHit hit;

        // logic: "Shoot from FirePoint, Forward direction, Store hit info, Max Distance, ONLY hit 'enemyLayer'"
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, freezeBeamRange, enemyLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.name); // See what we hit in Console!

            // Attempt to find the DroneAI script on the object we hit OR its parent
            DroneAI drone = hit.collider.GetComponentInParent<DroneAI>();

            if (drone != null)
            {
                drone.FreezeDrone();
                Debug.Log("Target Frozen!");
            }
        }
        else
        {
            Debug.Log("Missed! (Make sure the Enemy is on the correct Layer)");
        }
    }

    // Draw the attack range in the Editor so you can see it
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // This matches the radius in PerformAttack
        Gizmos.DrawWireSphere(transform.position, 3.0f);
    }
}