using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DroneMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform robot;
    private RobotMovement robotMovement;

    [Header("Combat & Aiming")]
    public LineRenderer laserLine;
    public LayerMask enemyLayer;
    public float freezeRange = 100f;
    public float beamRadius = 1.0f;
    public Transform crosshairVisual;
    public Transform laserOrigin;

    [Header("Energy Settings")]
    [SerializeField] float maxBeamEnergy = 100f;
    [SerializeField] float rechargeDuration = 5f;
    [SerializeField] float beamDepletionRate = 20f;
    [SerializeField] float coolingPower = 25f;
    private float currentBeamEnergy;

    [Header("Position Settings")]
    [SerializeField] Vector3 baseOffset = new Vector3(1.5f, 2.5f, 0f);

    [Header("Mouse Control")]
    [SerializeField] float sensitivityX = 0.5f;
    [SerializeField] float sensitivityY = 0.5f;

    [Header("Limits")]
    [SerializeField] float bodyMoveLimitX = 0.5f; // Keeps body on right
    [SerializeField] float bodyMoveLimitY = 1.0f;
    [SerializeField] float aimLimitX = 5.0f;      // Allows aim to go left
    [SerializeField] float aimLimitY = 4.0f;
    [SerializeField] float aimDistance = 20f;

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.1f;

    private Vector3 currentVelocity;
    private float mouseX;
    private float mouseY;
    private CharacterController droneController;

    void Start()
    {
        droneController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (robot != null)
        {
            robotMovement = robot.GetComponent<RobotMovement>();
            droneController.enabled = false;
            transform.position = robot.TransformPoint(baseOffset);
            droneController.enabled = true;
        }
        if (laserLine != null) laserLine.enabled = false;
    }

    // 1. READ INPUT HERE
    void Update()
    {
        if (Mouse.current == null || robot == null) return;

        // Collect Mouse Input
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        mouseX += mouseDelta.x * sensitivityX * Time.deltaTime;
        mouseY += mouseDelta.y * sensitivityY * Time.deltaTime;

        // Clamp Aim
        mouseX = Mathf.Clamp(mouseX, -aimLimitX, aimLimitX);
        mouseY = Mathf.Clamp(mouseY, -aimLimitY, aimLimitY);

        // Calculate Aim Target for visual crosshair updates
        Vector3 aimLocalPos = new Vector3(mouseX, mouseY + 2.0f, aimDistance);
        Vector3 aimWorldPos = robot.TransformPoint(aimLocalPos);

        if (crosshairVisual != null) crosshairVisual.position = aimWorldPos;

        // Combat Logic
        HandleCombat(aimWorldPos);
    }

    // 2. MOVE DRONE HERE (Prevents Jitter)
    void LateUpdate()
    {
        if (robot == null) return;

        // 1. POSITION LOGIC (Keep aiming independent of body pos)
        float droneX = Mathf.Clamp(mouseX, -bodyMoveLimitX, bodyMoveLimitX);
        float droneY = Mathf.Clamp(mouseY, -bodyMoveLimitY, bodyMoveLimitY);

        Vector3 droneLocalPos = baseOffset + new Vector3(droneX, droneY, 0);
        Vector3 droneWorldPos = robot.TransformPoint(droneLocalPos);

        // Move Smoothly
        Vector3 nextPosition = Vector3.SmoothDamp(transform.position, droneWorldPos, ref currentVelocity, smoothTime);
        Vector3 movementDelta = nextPosition - transform.position;
        droneController.Move(movementDelta);

        // 2. ROTATION LOGIC (Turret Aiming + 180 Correction)

        // Recalculate aim target
        Vector3 aimLocalPos = new Vector3(mouseX, mouseY + 2.0f, aimDistance);
        Vector3 aimWorldPos = robot.TransformPoint(aimLocalPos);

        // A. Find the direction to the crosshair
        Vector3 directionToTarget = aimWorldPos - transform.position;

        // B. Calculate the rotation to look at that target
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);

            // C. Apply your 180-degree offset HERE
            // This tells Unity: "Look at the target, then spin 180 degrees so the face is correct"
            Quaternion correctedRotation = lookRotation * Quaternion.Euler(0, 180, 0);

            // D. Apply smoothly
            transform.rotation = Quaternion.Slerp(transform.rotation, correctedRotation, Time.deltaTime * 10f);
        }
    }

    void HandleCombat(Vector3 targetPoint)
    {
        bool isCooling = Mouse.current.leftButton.isPressed;
        bool isFreezing = Mouse.current.rightButton.isPressed;

        if ((isCooling || isFreezing) && currentBeamEnergy > 0)
        {
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;

            if (isCooling)
            {
                if (robotMovement != null) robotMovement.ApplyCooling(coolingPower * Time.deltaTime);
                if (laserLine != null) laserLine.enabled = false;
            }
            else if (isFreezing)
            {
                FireFreezeBeam(targetPoint);
            }
        }
        else
        {
            currentBeamEnergy += (maxBeamEnergy / rechargeDuration) * Time.deltaTime;
            if (laserLine != null) laserLine.enabled = false;
        }
        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
    }

    void FireFreezeBeam(Vector3 targetPoint)
    {
        Vector3 startPos = laserOrigin.position;

        // Calculate direction from that specific point to the target
        Vector3 direction = (targetPoint - startPos).normalized;

        Vector3 laserEndPoint = startPos + direction * freezeRange;
        bool foundTarget = false;

        RaycastHit[] hits = Physics.SphereCastAll(startPos, beamRadius, direction, freezeRange, enemyLayer);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            DestroyableObject destObj = hit.collider.GetComponent<DestroyableObject>();
            if (destObj != null)
            {
                destObj.Freeze();
                laserEndPoint = hit.point;
                foundTarget = true;
                if (laserLine != null) laserLine.startColor = Color.cyan;
                break; // Stop raycast here
            }

            BossVent vent = hit.collider.GetComponent<BossVent>();
            if (vent != null)
            {
                vent.TakeLaserDamage(50f * Time.deltaTime);
                laserEndPoint = hit.point;
                foundTarget = true;
                if (laserLine != null) laserLine.startColor = Color.red;
                break;
            }

            BossAI boss = hit.collider.GetComponent<BossAI>();
            if (boss != null && !foundTarget)
            {
                boss.FreezeBoss();
                laserEndPoint = hit.point;
                foundTarget = true;
                if (laserLine != null) laserLine.startColor = Color.cyan;
            }

            DroneAI enemyDrone = hit.collider.GetComponent<DroneAI>();
            if (enemyDrone == null) enemyDrone = hit.collider.GetComponentInParent<DroneAI>();
            if (enemyDrone != null && !foundTarget)
            {
                enemyDrone.FreezeDrone();
                laserEndPoint = hit.point;
                foundTarget = true;
                break;
            }
        }

        if (laserLine != null)
        {
            laserLine.enabled = true;

            // CHANGE 2: Laser starts at the new Fire Point
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, laserEndPoint);

            if (!foundTarget) laserLine.startColor = Color.cyan;

        }
    }
}