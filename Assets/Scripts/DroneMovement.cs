using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
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

    [Header("--- VISUAL ENHANCEMENTS ---")] 
    [Tooltip("Particle system at the drone's nozzle")]
    public ParticleSystem muzzleFlashParticles; 
    [Tooltip("Particle system at the point where the beam hits")]
    public ParticleSystem impactParticles; 
    [Tooltip("Light source at the impact point for glow")]
    public Light impactLight; 
    
    [Header("Beam Animation")]
    public float textureScrollSpeed = 10f; // Speed of the beam flow
    public float beamNoiseScale = 0.5f;    // How "jagged" the beam is
    public int beamSegments = 20;          // Smoothness of the curve

    [Header("Audio Settings")]
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
        audioSource = GetComponent<AudioSource>();

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
        
        // Setup LineRenderer for animation
        if (laserLine != null)
        {
            laserLine.enabled = false;
            laserLine.positionCount = beamSegments; 
            laserLine.useWorldSpace = true;
        }

        // Initialize Visuals off
        if (muzzleFlashParticles) muzzleFlashParticles.Stop();
        if (impactParticles) impactParticles.Stop();
        if (impactLight) impactLight.enabled = false;
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

        // Drone Movement Logic
        float droneX = Mathf.Clamp(mouseX, -bodyMoveLimitX, bodyMoveLimitX);
        float droneY = Mathf.Clamp(mouseY, -bodyMoveLimitY, bodyMoveLimitY);

        Vector3 droneLocalPos = baseOffset + new Vector3(droneX, droneY, 0);
        Vector3 droneWorldPos = robot.TransformPoint(droneLocalPos);

        Vector3 nextPosition = Vector3.SmoothDamp(transform.position, droneWorldPos, ref currentVelocity, smoothTime);
        Vector3 movementDelta = nextPosition - transform.position;
        droneController.Move(movementDelta);

        // Rotation Logic
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
        bool isCoolingInput = Mouse.current.leftButton.isPressed;
        bool isFreezingInput = Mouse.current.rightButton.isPressed;

        // Determine if we have enough energy to perform actions
        bool hasEnergy = currentBeamEnergy > 0;
        
        // Prioritize Freeze Beam over Cooling if both buttons are pressed (or handle however you prefer)
        // Here: Right Click = Freeze Beam, Left Click = Cool Robot
        bool attemptingFreeze = isFreezingInput;
        bool attemptingCool = isCoolingInput && !isFreezingInput; // Prevent doing both at once if you want strict priority

        if (attemptingFreeze && hasEnergy)
        {
            // --- STATE 1: FIRING FREEZE BEAM ---
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;
            FireFreezeBeam(targetPoint);

            // Audio: Play Loop
            if (!audioSource.isPlaying && beamSound != null) audioSource.Play();
        }
        else if (attemptingCool && hasEnergy)
        {
            // --- STATE 2: COOLING ROBOT ---
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;
            
            // Logic: Apply cooling to robot
            if (robotMovement != null) robotMovement.ApplyCooling(coolingPower * Time.deltaTime);

            // Visuals: Turn off beam visuals while cooling
            DisableBeamVisuals();

            // Audio: Stop beam sound (unless you add a specific cooling sound)
            if (audioSource.isPlaying) audioSource.Stop();
        }
        else
        {
            // --- STATE 3: IDLE / RECHARGING ---
            // We are neither freezing nor cooling, so we recharge.
            currentBeamEnergy += (maxBeamEnergy / rechargeDuration) * Time.deltaTime;

            // Visuals: Turn off
            DisableBeamVisuals();

            // Audio: Stop
            if (audioSource.isPlaying) audioSource.Stop();
        }

        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
    }

    void DisableBeamVisuals()
    {
        if (laserLine != null) laserLine.enabled = false;
        if (muzzleFlashParticles) muzzleFlashParticles.Stop();
        if (impactParticles) impactParticles.Stop();
        if (impactLight) impactLight.enabled = false;
    }

    void FireFreezeBeam(Vector3 targetPoint)
    {
        Vector3 startPos = laserOrigin.position;
        Vector3 direction = (targetPoint - startPos).normalized;
        Vector3 laserEndPoint = startPos + direction * freezeRange;
        bool foundTarget = false;
        Color beamColor = Color.cyan; // Default color

        // Raycasting
        RaycastHit[] hits = Physics.SphereCastAll(startPos, beamRadius, direction, freezeRange, enemyLayer);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            // Logic for hitting different objects
            DestroyableObject destObj = hit.collider.GetComponent<DestroyableObject>();
            if (destObj != null)
            {
                destObj.Freeze();
                laserEndPoint = hit.point;
                foundTarget = true;
                beamColor = Color.cyan;
                break; 
            }

            BossVent vent = hit.collider.GetComponent<BossVent>();
            if (vent != null)
            {
                vent.TakeLaserDamage(50f * Time.deltaTime);
                laserEndPoint = hit.point;
                foundTarget = true;
                beamColor = Color.red; // Visual feedback for damage
                break;
            }

            BossAI boss = hit.collider.GetComponent<BossAI>();
            if (boss != null && !foundTarget)
            {
                boss.FreezeBoss();
                laserEndPoint = hit.point;
                foundTarget = true;
                beamColor = Color.cyan;
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

        // --- VISUAL UPDATE ---
        UpdateBeamVisuals(startPos, laserEndPoint, beamColor);
    }

    void UpdateBeamVisuals(Vector3 start, Vector3 end, Color color)
    {
        if (laserLine == null) return;

        laserLine.enabled = true;
        laserLine.startColor = color;
        laserLine.endColor = color;

        // 1. Texture Scrolling
        laserLine.material.mainTextureOffset = new Vector2(Time.time * textureScrollSpeed, 0);

        // 2. Muzzle Flash
        if (muzzleFlashParticles != null)
        {
            if (!muzzleFlashParticles.isPlaying) muzzleFlashParticles.Play();
            muzzleFlashParticles.transform.position = start;
        }

        // 3. Impact Visuals
        if (impactParticles != null)
        {
            impactParticles.transform.position = end;
            impactParticles.transform.LookAt(start); 
            if (!impactParticles.isPlaying) impactParticles.Play();
        }

        if (impactLight != null)
        {
            impactLight.enabled = true;
            impactLight.transform.position = end - (end - start).normalized * 0.5f; 
            impactLight.color = color;
        }

        // 4. Jitter / Noise
        float distance = Vector3.Distance(start, end);
        laserLine.positionCount = beamSegments;
        
        for (int i = 0; i < beamSegments; i++)
        {
            float t = (float)i / (beamSegments - 1); 
            Vector3 pos = Vector3.Lerp(start, end, t);

            if (i > 0 && i < beamSegments - 1)
            {
                Vector3 noise = Random.insideUnitSphere * beamNoiseScale;
                pos += noise;
            }

            laserLine.SetPosition(i, pos);
        }
    }
}