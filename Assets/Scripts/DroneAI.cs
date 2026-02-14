using UnityEngine;

public class DroneAI : MonoBehaviour
{
    public enum DroneState { Hovering, Diving, Attacking, Ascending }

    [Header("State")]
    public DroneState currentState = DroneState.Hovering;
    public bool IsFrozen = false;

    [Header("Movement Settings")]
    public float flySpeed = 5.0f;       
    public float diveSpeed = 12.0f;      
    public float hoverHeight = 3.0f;    
    public float attackHeight = 1.0f;   

    [Header("Timing")]
    public float hoverTime = 2.0f;
    public float attackTime = 1.0f;
    private float timer;

    [Header("Combat")]
    public float heatDamage = 10.0f;
    public GameObject explosionPrefab;

    private Transform player;
    // We remove the direct RobotMovement reference if not strictly needed for heat, 
    // but assuming you have it from before:
    // private RobotMovement playerScript; 
    private Rigidbody rb;

    [Header("Ambush Settings")]
    public bool startsActive = true; 
    private bool isActive = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            // playerScript = player.GetComponent<RobotMovement>();
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        timer = hoverTime;
        isActive = startsActive;
    }

    void Update()
    {
        if (!isActive || IsFrozen || player == null) return;

        switch (currentState)
        {
            case DroneState.Hovering:
                HandleHover();
                break;
            case DroneState.Diving:
                HandleDive();
                break;
            case DroneState.Attacking:
                HandleAttack();
                break;
            case DroneState.Ascending:
                HandleAscend();
                break;
        }
    }

    // --- MOVEMENT LOGIC (Simplified for brevity, same as your original) ---
    void HandleHover()
    {
        Vector3 highPoint = player.position + Vector3.up * hoverHeight;
        transform.position = Vector3.MoveTowards(transform.position, highPoint, flySpeed * Time.deltaTime);
        transform.LookAt(player);
        
        timer -= Time.deltaTime;
        if (timer <= 0) currentState = DroneState.Diving;
    }

    void HandleDive()
    {
        Vector3 attackPoint = player.position + Vector3.up * attackHeight;
        transform.position = Vector3.MoveTowards(transform.position, attackPoint, diveSpeed * Time.deltaTime);
        transform.LookAt(player);

        if (Vector3.Distance(transform.position, attackPoint) < 0.5f)
        {
            currentState = DroneState.Attacking;
            timer = attackTime;
        }
    }

    void HandleAttack()
    {
        // Attack logic here (Heating player)
        // if (playerScript != null) playerScript.IncreaseHeat(heatDamage * Time.deltaTime);
        
        timer -= Time.deltaTime;
        if (timer <= 0) currentState = DroneState.Ascending;
    }

    void HandleAscend()
    {
        Vector3 highPoint = player.position + Vector3.up * hoverHeight;
        transform.position = Vector3.MoveTowards(transform.position, highPoint, flySpeed * Time.deltaTime);
        transform.LookAt(player);

        if (Vector3.Distance(transform.position, highPoint) < 0.5f)
        {
            currentState = DroneState.Hovering;
            timer = hoverTime;
        }
    }

    public void FreezeDrone()
    {
        if (IsFrozen) return;
        IsFrozen = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = Color.cyan;
    }

    // --- UPDATED FUNCTION ---
    public void SmashDrone()
    {
        // 1. Notify GameManager (Score, etc.)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyDefeated();
        }

        // 2. NEW: Update Objective to "Proceed to Exit"
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.UpdateObjective("Destroy Fusebox To Exit");
        }

        // 3. FX and Destruction
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void WakeUp()
    {
        isActive = true;
        Debug.Log("Drone Active!");
    }
}