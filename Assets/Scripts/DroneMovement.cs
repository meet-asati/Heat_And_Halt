using UnityEngine;
using UnityEngine.InputSystem;

// Automatically adds a CharacterController to the drone
[RequireComponent(typeof(CharacterController))]
public class DroneMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform robot;

    [Header("Freeze Beam Settings")]
    [SerializeField] float maxBeamEnergy = 100f;
    [Tooltip("Time in seconds to fully recharge the beam")]
    [SerializeField] float rechargeDuration = 5f;
    private float currentBeamEnergy;

    [Header("Position Settings")]
    [SerializeField] Vector3 baseOffset = new Vector3(1.5f, 1.8f, 0f); 

    [Header("Mouse Control")]
    [SerializeField] float sensitivityX = 0.5f;
    [SerializeField] float sensitivityY = 0.5f;

    [Header("Boundaries")]
    [SerializeField] float horizontalLimit = 0.5f; 
    [SerializeField] float verticalLimit = 0.8f;

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.1f;

    private Vector3 currentVelocity;
    private float offsetX;
    private float offsetY;
    
    // Reference to the collision component
    private CharacterController droneController;

    void Start()
    {
        // Get the CharacterController component
        droneController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize position to start near the robot
        if (robot != null)
        {
             // We disable the controller momentarily to teleport it to the start position
             droneController.enabled = false;
             transform.position = robot.TransformPoint(baseOffset);
             droneController.enabled = true;
        }

        currentBeamEnergy = 0f;
    }

    void Update()
    {
        // --- 1. Freeze Beam Logic (Recharge) ---
        float rechargeRate = maxBeamEnergy / rechargeDuration;
        currentBeamEnergy += rechargeRate * Time.deltaTime;
        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
        }
    }

    void LateUpdate()
    {
        if (robot == null || Mouse.current == null) return;

        // --- 2. Calculate Target Position ---
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        offsetX += mouseDelta.x * sensitivityX * Time.deltaTime;
        offsetY += mouseDelta.y * sensitivityY * Time.deltaTime;

        offsetX = Mathf.Clamp(offsetX, -horizontalLimit, horizontalLimit);
        offsetY = Mathf.Clamp(offsetY, -verticalLimit, verticalLimit);

        Vector3 targetLocalPos = baseOffset + new Vector3(offsetX, offsetY, 0);
        Vector3 targetWorldPos = robot.TransformPoint(targetLocalPos);

        // --- 3. Move with Collision (The Fix) ---
        // Instead of setting position directly, we calculate the next step using SmoothDamp
        Vector3 nextPosition = Vector3.SmoothDamp(transform.position, targetWorldPos, ref currentVelocity, smoothTime);
        
        // Calculate the difference (delta) between where we are and where we want to be
        Vector3 movementDelta = nextPosition - transform.position;

        // Use Move() so the CharacterController handles wall collisions automatically
        droneController.Move(movementDelta);

        // --- 4. Handle Rotation ---
        Quaternion targetRot = robot.rotation * Quaternion.Euler(0, 180, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }
}