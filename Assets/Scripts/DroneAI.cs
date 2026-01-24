using UnityEngine;

public class DroneAI : MonoBehaviour
{
    public enum DroneState { Hovering, Diving, Attacking, Ascending }

    [Header("State")]
    public DroneState currentState = DroneState.Hovering;
    public bool IsFrozen = false;

    [Header("Movement Settings")]
    public float flySpeed = 10.0f;       // Increased from 5
    public float diveSpeed = 20.0f;      // Increased from 12  
    public float hoverHeight = 6.0f;    // High wait position
    public float attackHeight = 1.5f;   // Attack position (Chest/Head)

    [Header("Timing")]
    public float hoverTime = 2.0f;
    public float attackTime = 1.0f;
    private float timer;

    [Header("Combat")]
    public float heatDamage = 10.0f;
    public GameObject explosionPrefab;

    private Transform player;
    private RobotMovement playerScript;
    private Rigidbody rb;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerScript = player.GetComponent<RobotMovement>();
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        timer = hoverTime;
    }

    void Update()
    {
        if (IsFrozen || player == null) return;

        // Calculate the specific point we want to look at (Chest/Head)
        // We use the same height as our attack target
        Vector3 lookTarget = player.position + Vector3.up * attackHeight;

        switch (currentState)
        {
            case DroneState.Hovering:
                HandleHover(lookTarget);
                break;
            case DroneState.Diving:
                HandleDive(lookTarget);
                break;
            case DroneState.Attacking:
                HandleAttack(lookTarget);
                break;
            case DroneState.Ascending:
                HandleAscend(lookTarget);
                break;
        }
    }

    void HandleHover(Vector3 lookSpot)
    {
        Vector3 highPoint = player.position + Vector3.up * hoverHeight;

        transform.position = Vector3.MoveTowards(transform.position, highPoint, flySpeed * Time.deltaTime);

        // FIX: Look at the player's chest, not feet
        transform.LookAt(lookSpot);

        timer -= Time.deltaTime;
        if (timer <= 0) currentState = DroneState.Diving;
    }

    void HandleDive(Vector3 lookSpot)
    {
        Vector3 attackPoint = player.position + Vector3.up * attackHeight;

        transform.position = Vector3.MoveTowards(transform.position, attackPoint, diveSpeed * Time.deltaTime);
        transform.LookAt(lookSpot); // Keep eyes on the chest

        float dist = Vector3.Distance(transform.position, attackPoint);
        if (dist < 0.5f)
        {
            currentState = DroneState.Attacking;
            timer = attackTime;
        }
    }

    void HandleAttack(Vector3 lookSpot)
    {
        Vector3 attackPoint = player.position + Vector3.up * attackHeight;

        transform.position = Vector3.MoveTowards(transform.position, attackPoint, flySpeed * Time.deltaTime);
        transform.LookAt(lookSpot); // Ensure gun points at chest

        if (playerScript != null) playerScript.IncreaseHeat(heatDamage * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer <= 0) currentState = DroneState.Ascending;
    }

    void HandleAscend(Vector3 lookSpot)
    {
        Vector3 highPoint = player.position + Vector3.up * hoverHeight;

        transform.position = Vector3.MoveTowards(transform.position, highPoint, flySpeed * Time.deltaTime);
        transform.LookAt(lookSpot); // Still watching player while leaving

        float dist = Vector3.Distance(transform.position, highPoint);
        if (dist < 0.5f)
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
            rb.constraints = RigidbodyConstraints.None;
            // Add a little spin so it looks like a crash
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = Color.cyan;
    }

    public void SmashDrone()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemyDefeated();
        }

        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}