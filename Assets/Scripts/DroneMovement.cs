using UnityEngine;
using UnityEngine.InputSystem;

public class DroneMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform robot;

    [Header("Freeze Beam Settings")]
    [SerializeField] float maxBeamEnergy = 100f;
    [Tooltip("Time in seconds to fully recharge the beam from empty")]
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

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (robot != null)
        {
             transform.position = robot.TransformPoint(baseOffset);
        }
        
        // Start with empty energy (or change to maxBeamEnergy if you want it full at start)
        currentBeamEnergy = 0f;
    }

    void Update() 
    {
        // --- Freeze Beam Logic ---
        // Calculate how much energy to add per second to match duration
        // Formula: Rate = Max / Time
        float rechargeRate = maxBeamEnergy / rechargeDuration;
        
        // Recharge over time
        currentBeamEnergy += rechargeRate * Time.deltaTime;
        
        // Clamp energy
        currentBeamEnergy = Mathf.Clamp(currentBeamEnergy, 0, maxBeamEnergy);

        // UPDATE UI
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateFreezeBar(currentBeamEnergy, maxBeamEnergy);
        }
    }

    void LateUpdate()
    {
        if (robot == null || Mouse.current == null) return;

        // --- Existing Movement Logic ---
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        offsetX += mouseDelta.x * sensitivityX * Time.deltaTime;
        offsetY += mouseDelta.y * sensitivityY * Time.deltaTime;

        offsetX = Mathf.Clamp(offsetX, -horizontalLimit, horizontalLimit);
        offsetY = Mathf.Clamp(offsetY, -verticalLimit, verticalLimit);

        Vector3 targetLocalPos = baseOffset + new Vector3(offsetX, offsetY, 0);
        Vector3 targetWorldPos = robot.TransformPoint(targetLocalPos);

        transform.position = Vector3.SmoothDamp(transform.position, targetWorldPos, ref currentVelocity, smoothTime);

        Quaternion targetRot = robot.rotation * Quaternion.Euler(0,180,0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }
}
