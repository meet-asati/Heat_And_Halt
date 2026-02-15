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
    public ParticleSystem muzzleFlashParticles; 
    public ParticleSystem impactParticles; 
    public Light impactLight; 
    
    [Header("Beam Animation")]
    public float textureScrollSpeed = 10f; 
    public float beamNoiseScale = 0.5f;    
    public int beamSegments = 20;          

    [Header("Audio Settings")]
    public AudioClip beamSound;
    private AudioSource audioSource;

    [Header("Energy Settings")]
    [SerializeField] float maxBeamEnergy = 100f;
    [SerializeField] float rechargeDuration = 5f;
    [SerializeField] float beamDepletionRate = 20f;
    [SerializeField] float coolingPower = 25f;
    private float currentBeamEnergy;

    // --- LOGIC VARIABLE ---
    private bool requiresReset = false; // The "Latch" for the manual reset mechanic

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

        // --- CHANGE HERE: START EMPTY ---
        currentBeamEnergy = 0f; 
        // The Update loop will automatically start filling it because you aren't pressing buttons yet.

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (robot != null)
        {
            robotMovement = robot.GetComponent<RobotMovement>();
            droneController.enabled = false;
            transform.position = robot.TransformPoint(baseOffset);
            droneController.enabled = true;
        }
        
        if (laserLine != null)
        {
            laserLine.enabled = false;
            laserLine.positionCount = beamSegments; 
            laserLine.useWorldSpace = true;
        }

        DisableBeamVisuals();
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
        bool isCoolingInput = Mouse.current.leftButton.isPressed;
        bool isFreezingInput = Mouse.current.rightButton.isPressed;

        // =========================================================
        // MANUAL RESET LOGIC
        // =========================================================

        // 1. Check if we hit empty
        if (currentBeamEnergy <= 0f)
        {
            requiresReset = true; // Lock the gun
        }

        // 2. Check for Release
        // Unlock ONLY if player lets go of buttons
        if (!isCoolingInput && !isFreezingInput)
        {
            requiresReset = false;
        }

        // 3. Permission to Fire
        bool canFire = (currentBeamEnergy > 0f) && (!requiresReset);

        // =========================================================

        bool attemptingFreeze = isFreezingInput;
        bool attemptingCool = isCoolingInput && !isFreezingInput; 

        if (attemptingFreeze && canFire)
        {
            // --- STATE 1: FIRING FREEZE BEAM ---
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;
            FireFreezeBeam(targetPoint);

            if (!audioSource.isPlaying && beamSound != null) audioSource.Play();
        }
        else if (attemptingCool && canFire)
        {
            // --- STATE 2: COOLING ROBOT ---
            currentBeamEnergy -= beamDepletionRate * Time.deltaTime;
            
            if (robotMovement != null) robotMovement.ApplyCooling(coolingPower * Time.deltaTime);

            DisableBeamVisuals();
            
            if (audioSource.isPlaying) audioSource.Stop();
        }
        else
        {
            // --- STATE 3: IDLE / RECHARGING ---
            // Recharges automatically when not firing or when locked
            currentBeamEnergy += (maxBeamEnergy / rechargeDuration) * Time.deltaTime;

            DisableBeamVisuals();
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
        Color beamColor = Color.cyan; 

        RaycastHit[] hits = Physics.SphereCastAll(startPos, beamRadius, direction, freezeRange, enemyLayer);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        bool foundTarget = false;

        foreach (RaycastHit hit in hits)
        {
            DestroyableObject destObj = hit.collider.GetComponent<DestroyableObject>();
            if (destObj != null)
            {
                destObj.Freeze();
                laserEndPoint = hit.point;
                foundTarget = true;
                break; 
            }

            BossVent vent = hit.collider.GetComponent<BossVent>();
            if (vent != null)
            {
                vent.TakeLaserDamage(50f * Time.deltaTime);
                laserEndPoint = hit.point;
                beamColor = Color.red; 
                foundTarget = true;
                break;
            }

            BossAI boss = hit.collider.GetComponent<BossAI>();
            if (boss != null && !foundTarget)
            {
                boss.FreezeBoss();
                laserEndPoint = hit.point;
                foundTarget = true;
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

        UpdateBeamVisuals(startPos, laserEndPoint, beamColor);
    }

    void UpdateBeamVisuals(Vector3 start, Vector3 end, Color color)
    {
        if (laserLine == null) return;

        laserLine.enabled = true;
        laserLine.startColor = color;
        laserLine.endColor = color;

        laserLine.material.mainTextureOffset = new Vector2(Time.time * textureScrollSpeed, 0);

        if (muzzleFlashParticles != null)
        {
            if (!muzzleFlashParticles.isPlaying) muzzleFlashParticles.Play();
            muzzleFlashParticles.transform.position = start;
        }

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