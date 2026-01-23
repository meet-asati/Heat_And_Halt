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
    }

    public void ApplyCooling(float coolingAmount)
    {
        CurrentHeat -= coolingAmount;
        CurrentHeat = Mathf.Clamp(CurrentHeat, 0, maxHeat);
    }

      System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;  // 1. Lock movement
        
        robotAnimator.SetTrigger("Fight"); // 2. Play Animation
        Debug.Log("Attack Started - Movement Locked");

        // 3. Wait for the animation to finish
        yield return new WaitForSeconds(attackDuration);

        isAttacking = false; // 4. Unlock movement
        Debug.Log("Attack Finished - Movement Restored");
    }

}