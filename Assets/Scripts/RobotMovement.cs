using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float reverseSpeed = 3f;
    [SerializeField] float turnSpeed = 120f;

    [Header("Heat Settings")]
    [SerializeField] float maxHeat = 100f;
    [Tooltip("Heat added per second while walking")]
    [SerializeField] float walkHeatRate = 5f;
    [Tooltip("Heat added per second while sprinting")]
    [SerializeField] float sprintHeatRate = 15f; 
    
    // Tracks current heat
    public float CurrentHeat { get; private set; } 

    [Header("Animation Settings")]
    [SerializeField] float dampTime = 0.1f;

    private Animator robotAnimator;

    void Start()
    {
        robotAnimator = GetComponent<Animator>();
        CurrentHeat = 0f; // Start with 0 heat
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // --- 1. Read Inputs ---
        float moveInput = 0f;
        if (Keyboard.current.wKey.isPressed) moveInput += 1f;
        if (Keyboard.current.sKey.isPressed) moveInput -= 1f;

        float turnInput = 0f;
        if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
        if (Keyboard.current.dKey.isPressed) turnInput += 1f;

        // --- 2. Handle Rotation ---
        if (turnInput != 0)
        {
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 3. Handle Movement & Heat ---
        bool isSprinting = (moveInput > 0) && Keyboard.current.leftShiftKey.isPressed;
        
        float currentSpeed = 0f;
        float animValue = 0f;

        // Logic: Calculate Heat Generation based on movement
        if (moveInput != 0)
        {
            // Determine heat rate: Higher for sprint, lower for walk/reverse
            float heatToAdd = isSprinting ? sprintHeatRate : walkHeatRate;
            
            // Add heat over time
            CurrentHeat += heatToAdd * Time.deltaTime;
        }

        // Clamp Heat so it doesn't exceed Max
        CurrentHeat = Mathf.Clamp(CurrentHeat, 0, maxHeat);

        // UPDATE UI: Send data to HUDManager
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHeatBar(CurrentHeat, maxHeat);
        }

        // -- Existing Movement Logic --
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

        if (moveInput != 0)
        {
            Vector3 moveDirection = transform.forward * moveInput * currentSpeed * Time.deltaTime;
            transform.position += moveDirection;
        }

        // --- 4. Update Animator ---
        if(robotAnimator != null)
            robotAnimator.SetFloat("Speed", Mathf.Abs(animValue), dampTime, Time.deltaTime);
    }

    // Call this from Drone's freeze beam to cool down
    public void ApplyCooling(float coolingAmount)
    {
        CurrentHeat -= coolingAmount;
        CurrentHeat = Mathf.Clamp(CurrentHeat, 0, maxHeat);
    }
}


