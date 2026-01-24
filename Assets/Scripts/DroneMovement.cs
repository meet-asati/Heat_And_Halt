using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DroneMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform robot;
    private RobotMovement robotMovement; // REFERENCE TO ROBOT SCRIPT

    [Header("Freeze Beam & Combat")] // --- NEW SECTION ---
    public LineRenderer laserLine;       // Drag your Line Renderer here
    public LayerMask enemyLayer;         // Set to "Enemy" layer
    public float freezeRange = 100f;     // Distance of the beam

    [Header("Energy Settings")]
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

        currentBeamEnergy = 0f; // Start empty or full depending on preference

        // --- NEW: Turn off laser at start ---
        if (laserLine != null) laserLine.enabled = false;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // --- 1. Handle Input States ---
        bool isCooling = Mouse.current.leftButton.isPressed; // LMB: Cool Robot
        bool isFreezing = Mouse.current.rightButton.isPressed; // RMB: Freeze Enemy & Aim

        // Set aiming state for movement smoothing (Existing mechanic)
        isAiming = isFreezing;

        // --- 2. Action Logic ---
        // Strict Check: Must have energy > 0 to start OR continue firing
        if ((isCooling || isFreezing) && currentBeamEnergy > 0)
        {
            // Drain energy
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;

            if (isCooling)
            {
                if (robotMovement != null) robotMovement.ApplyCooling(coolingPower * Time.deltaTime);
                if (laserLine != null) laserLine.enabled = false; // Priority to cooling, no laser
            }
            else if (isFreezing)
            {
                FireFreezeBeam();
            }
        }
        else
        {
            // --- RECHARGE STATE ---
            // If we are here, we either released the button OR ran out of energy
            float rechargeRate = maxBeamEnergy / rechargeDuration;
            currentBeamEnergy += rechargeRate * Time.deltaTime;

            // CUT THE LASER IMMEDIATELY
            if (laserLine != null) laserLine.enabled = false;
        }

        // Clamp Energy
        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        // Update UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
        }
    }

    // --- NEW: SHOOTING LOGIC ---
    void FireFreezeBeam()
    {
        // 1. AIM: Raycast from the Camera Center (Player's Eye)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Default end point if we miss (shoot into sky)
        Vector3 laserEndPoint = ray.GetPoint(freezeRange);

        // 2. DETECT: Did the Camera see an enemy?
        if (Physics.Raycast(ray, out hit, freezeRange, enemyLayer))
        {
            laserEndPoint = hit.point; // Update target to actual hit

            // Try to find the Enemy Drone script
            DroneAI enemyDrone = hit.collider.GetComponent<DroneAI>();
            if (enemyDrone == null) enemyDrone = hit.collider.GetComponentInParent<DroneAI>();

            if (enemyDrone != null)
            {
                enemyDrone.FreezeDrone(); // Call the Freeze function on the enemy
            }
        }

        // 3. VISUALS: Draw line from COMPANION DRONE to TARGET
        if (laserLine != null)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, transform.position); // Start at Drone
            laserLine.SetPosition(1, laserEndPoint);      // End at Enemy/Wall
        }
    }

    void LateUpdate()
    {
        if (robot == null || Mouse.current == null) return;

        // --- 3. Aim Mode (RMB) ---
        // When aiming, we reduce sensitivity for precision
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