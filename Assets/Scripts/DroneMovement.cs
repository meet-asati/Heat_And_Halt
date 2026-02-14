using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))] // Added AudioSource requirement
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

    [Header("Audio Settings")] // NEW SECTION
    public AudioClip beamSound;
    private AudioSource audioSource;

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
    [SerializeField] float bodyMoveLimitX = 0.5f;
    [SerializeField] float bodyMoveLimitY = 1.0f;
    [SerializeField] float aimLimitX = 5.0f;
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
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource

        // Configure Audio Source for continuous beam
        audioSource.loop = true; 
        audioSource.playOnAwake = false;
        if (beamSound != null) audioSource.clip = beamSound;

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

    void Update()
    {
        if (Mouse.current == null || robot == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        mouseX += mouseDelta.x * sensitivityX * Time.deltaTime;
        mouseY += mouseDelta.y * sensitivityY * Time.deltaTime;

        mouseX = Mathf.Clamp(mouseX, -aimLimitX, aimLimitX);
        mouseY = Mathf.Clamp(mouseY, -aimLimitY, aimLimitY);

        Vector3 aimLocalPos = new Vector3(mouseX, mouseY + 2.0f, aimDistance);
        Vector3 aimWorldPos = robot.TransformPoint(aimLocalPos);

        if (crosshairVisual != null) crosshairVisual.position = aimWorldPos;

        HandleCombat(aimWorldPos);
    }

    void LateUpdate()
    {
        if (robot == null) return;

        float droneX = Mathf.Clamp(mouseX, -bodyMoveLimitX, bodyMoveLimitX);
        float droneY = Mathf.Clamp(mouseY, -bodyMoveLimitY, bodyMoveLimitY);

        Vector3 droneLocalPos = baseOffset + new Vector3(droneX, droneY, 0);
        Vector3 droneWorldPos = robot.TransformPoint(droneLocalPos);

        Vector3 nextPosition = Vector3.SmoothDamp(transform.position, droneWorldPos, ref currentVelocity, smoothTime);
        Vector3 movementDelta = nextPosition - transform.position;
        droneController.Move(movementDelta);

        Vector3 aimLocalPos = new Vector3(mouseX, mouseY + 2.0f, aimDistance);
        Vector3 aimWorldPos = robot.TransformPoint(aimLocalPos);
        Vector3 directionToTarget = aimWorldPos - transform.position;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            Quaternion correctedRotation = lookRotation * Quaternion.Euler(0, 180, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, correctedRotation, Time.deltaTime * 10f);
        }
    }

    void HandleCombat(Vector3 targetPoint)
    {
        bool isCooling = Mouse.current.leftButton.isPressed;
        bool isFreezing = Mouse.current.rightButton.isPressed;

        // CHECK: Is the beam actually firing? (Right Click + Has Energy)
        bool isFiringBeam = isFreezing && currentBeamEnergy > 0;

        if ((isCooling || isFiringBeam) && currentBeamEnergy > 0)
        {
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;

            if (isCooling)
            {
                if (robotMovement != null) robotMovement.ApplyCooling(coolingPower * Time.deltaTime);
                if (laserLine != null) laserLine.enabled = false;
                
                // Stop audio if we switch to cooling (unless you want cooling audio too)
                if (audioSource.isPlaying) audioSource.Stop(); 
            }
            else if (isFiringBeam)
            {
                FireFreezeBeam(targetPoint);

                // AUDIO LOGIC: Play loop if not already playing
                if (!audioSource.isPlaying && beamSound != null)
                {
                    audioSource.Play();
                }
            }
        }
        else
        {
            // RECHARGING / IDLE
            currentBeamEnergy += (maxBeamEnergy / rechargeDuration) * Time.deltaTime;
            if (laserLine != null) laserLine.enabled = false;

            // AUDIO LOGIC: Stop sound if we release button OR run out of energy
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        
        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
    }

    void FireFreezeBeam(Vector3 targetPoint)
    {
        Vector3 startPos = laserOrigin.position;
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
                break; 
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
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, laserEndPoint);

            if (!foundTarget) laserLine.startColor = Color.cyan;
        }
    }
}