using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Robot GameObject here")]
    [SerializeField] Transform robot;

    [Header("Position Settings")]
    [Tooltip("The default resting spot relative to the Robot (Right side)")]
    [SerializeField] Vector3 baseOffset = new Vector3(1.5f, 1.8f, 0f); 

    [Header("Mouse Control")]
    [SerializeField] float sensitivityX = 0.5f;
    [SerializeField] float sensitivityY = 0.5f;

    [Header("Boundaries (Relative to Base Offset)")]
    [Tooltip("How far right/left the drone can drift from its base spot")]
    [SerializeField] float horizontalLimit = 0.5f; 
    [Tooltip("How far up/down the drone can drift")]
    [SerializeField] float verticalLimit = 0.8f;

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.1f; // Lower = snappier, Higher = floatier

    private Vector3 currentVelocity;
    private float offsetX;
    private float offsetY;

    void Start()
    {
        // Optional: Lock cursor so mouse doesn't hit screen edges
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize position immediately to avoid a 'flying in' effect at start
        if (robot != null)
        {
             transform.position = robot.TransformPoint(baseOffset);
        }
    }

    void LateUpdate()
    {
        if (robot == null || Mouse.current == null) return;

        // --- 1. Get Mouse Input ---
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // --- 2. Calculate Offsets ---
        // Accumulate mouse movement into offset variables
        offsetX += mouseDelta.x * sensitivityX * Time.deltaTime;
        offsetY += mouseDelta.y * sensitivityY * Time.deltaTime;

        // --- 3. Apply Boundaries (Clamping) ---
        // This creates the "Invisible Boundary" box
        offsetX = Mathf.Clamp(offsetX, -horizontalLimit, horizontalLimit);
        offsetY = Mathf.Clamp(offsetY, -verticalLimit, verticalLimit);

        // --- 4. Calculate Target Position ---
        // Determine where the drone SHOULD be in local space (Robot's perspective)
        // We add the dynamic mouse offsets to the static base offset
        Vector3 targetLocalPos = baseOffset + new Vector3(offsetX, offsetY, 0);

        // Convert that local position to a World position based on the Robot's current spot
        // This handles the "Move forward at exact same speed" automatically
        Vector3 targetWorldPos = robot.TransformPoint(targetLocalPos);

        // --- 5. Apply Movement ---
        // SmoothDamp moves the drone smoothly to the target
        transform.position = Vector3.SmoothDamp(transform.position, targetWorldPos, ref currentVelocity, smoothTime);

        // --- 6. Handle Rotation ---
        // Drone always faces the same way as the robot
        Quaternion targetRot = robot.rotation * Quaternion.Euler(0,180,0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }
}
