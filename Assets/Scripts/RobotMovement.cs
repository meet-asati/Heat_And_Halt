using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float reverseSpeed = 3f; // Slower speed for backing up
    [SerializeField] float turnSpeed = 120f;  // How fast the robot rotates (Degrees/Sec)

    [Header("Animation Settings")]
    [SerializeField] float dampTime = 0.1f;

    private Animator robotAnimator;

    void Start()
    {
        robotAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // --- 1. Read Inputs ---
        // Vertical (W/S): Controls Forward/Backward movement
        float moveInput = 0f;
        if (Keyboard.current.wKey.isPressed) moveInput += 1f;
        if (Keyboard.current.sKey.isPressed) moveInput -= 1f;

        // Horizontal (A/D): Controls Rotation (Turning)
        float turnInput = 0f;
        if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
        if (Keyboard.current.dKey.isPressed) turnInput += 1f;

        // --- 2. Handle Rotation (A/D) ---
        // We rotate the robot around the Y-axis (Up).
        // Since the camera is parented/fixed to the back, it will rotate with the robot.
        if (turnInput != 0)
        {
            float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        // --- 3. Handle Movement (W/S) ---
        // Check for Sprint
        bool isSprinting = (moveInput > 0) && Keyboard.current.leftShiftKey.isPressed;
        
        // Determine current speed
        float currentSpeed = 0f;
        float animValue = 0f; // 0 = Idle, 0.5 = Walk, 1 = Run

        if (moveInput > 0) // Moving Forward
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
        else if (moveInput < 0) // Moving Backward
        {
            currentSpeed = reverseSpeed;
            animValue = -0.5f; // Optional: If you have a backward animation, or keep it 0.5f
        }

        // Apply Movement
        // transform.forward moves relative to where the robot is currently facing
        if (moveInput != 0)
        {
            // Note: We use moveInput (1 or -1) to determine direction
            // But we multiply by Time.deltaTime and Speed for distance
            Vector3 moveDirection = transform.forward * moveInput * currentSpeed * Time.deltaTime;
            transform.position += moveDirection;
        }

        // --- 4. Update Animator ---
        // We use Math.Abs for animValue because usually "Walk" animation is positive
        // If you have a specific "Walk Back" animation, remove Mathf.Abs
        robotAnimator.SetFloat("Speed", Mathf.Abs(animValue), dampTime, Time.deltaTime);
    }
}