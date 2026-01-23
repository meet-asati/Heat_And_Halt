using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DroneMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform robot;
    private RobotMovement robotMovement; // REFERENCE TO ROBOT SCRIPT

    [Header("Freeze Beam Settings")]
    [SerializeField] float maxBeamEnergy = 100f;
    [SerializeField] float rechargeDuration = 5f;
    [SerializeField] float beamDepletionRate = 20f; // Energy cost per second
    [SerializeField] float coolingPower = 25f;      // How much heat triggers reduction per second
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
    private CharacterController droneController;

    // AIMING STATE
    private bool isAiming = false;

    void Start()
    {
        droneController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize position
        if (robot != null)
        {
             // GET THE ROBOT SCRIPT REFERENCE HERE
             robotMovement = robot.GetComponent<RobotMovement>();

             droneController.enabled = false;
             transform.position = robot.TransformPoint(baseOffset);
             droneController.enabled = true;
        }

        currentBeamEnergy = 0f; // Start empty or full depending on preference (usually 0 to recharge)
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // --- 1. Handle Input States ---
        bool isFiring = Mouse.current.leftButton.isPressed;
        isAiming = Mouse.current.rightButton.isPressed;

        // --- 2. Cooling Logic (LMB) ---
        // Only cool if button pressed AND we have energy
        if (isFiring && currentBeamEnergy > 0)
        {
            // Drain the drone's energy
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;

            // Apply cooling to the robot
            if (robotMovement != null)
            {
                robotMovement.ApplyCooling(coolingPower * Time.deltaTime);
            }
        }
        else
        {
            // Recharge Logic (Only if not firing)
            float rechargeRate = maxBeamEnergy / rechargeDuration;
            currentBeamEnergy += rechargeRate * Time.deltaTime;
        }

        // Clamp Energy
        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        // Update UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
        }
    }

    void LateUpdate()
    {
        if (robot == null || Mouse.current == null) return;

        // --- 3. Aim Mode (RMB) ---
        // When aiming, we reduce sensitivity for precision (Placeholder for full aim mechanic)
        float currentSensX = isAiming ? sensitivityX * 0.5f : sensitivityX;
        float currentSensY = isAiming ? sensitivityY * 0.5f : sensitivityY;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        offsetX += mouseDelta.x * currentSensX * Time.deltaTime;
        offsetY += mouseDelta.y * currentSensY * Time.deltaTime;

        offsetX = Mathf.Clamp(offsetX, -horizontalLimit, horizontalLimit);
        offsetY = Mathf.Clamp(offsetY, -verticalLimit, verticalLimit);

        Vector3 targetLocalPos = baseOffset + new Vector3(offsetX, offsetY, 0);
        Vector3 targetWorldPos = robot.TransformPoint(targetLocalPos);

        // Movement with Smoothing
        Vector3 nextPosition = Vector3.SmoothDamp(transform.position, targetWorldPos, ref currentVelocity, smoothTime);
        Vector3 movementDelta = nextPosition - transform.position;
        droneController.Move(movementDelta);

        // Rotation
        Quaternion targetRot = robot.rotation * Quaternion.Euler(0, 180, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }
}