using UnityEngine;

public class DroneAI : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 3.0f;
    public float attackRange = 4.0f; // Updated to match previous fix
    public float heatDamage = 5.0f;
    public float hoverHeight = 2.0f; // Updated to match previous fix

    [Header("Flying Animation")]
    public float bobbingSpeed = 2.0f; // How fast it bobs up/down
    public float bobbingAmount = 0.5f; // How far it moves up/down

    [Header("Status")]
    public bool IsFrozen = false;

    private Transform player;
    private RobotMovement playerScript;
    private Rigidbody rb;

    void Start()
    {
        // 1. Find Player safely
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerScript = player.GetComponent<RobotMovement>();
        }

        // 2. Setup Physics
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearDamping = 1f; // Unity 6 (formerly 'drag')
            rb.angularDamping = 1f; // Unity 6 (formerly 'angularDrag')
            
            // Important: Keep Kinematic OFF so it detects collisions, 
            // but Gravity OFF so it flies.
            rb.isKinematic = false; 
        }
    }

    void Update()
    {
        if (IsFrozen || player == null || playerScript == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // --- 1. Calculate Target Position ---
        Vector3 targetPosition = player.position;
        
        // Add Hover Height + Sine Wave Bobbing for "Flying" effect
        float newY = hoverHeight + (Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount);
        targetPosition.y += newY; 

        // --- 2. Move Drone ---
        if (distance > attackRange)
        {
            // Move towards the player's calculated hover spot
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            transform.LookAt(player);
        }
        else 
        {
            // Attack: Stay in place (bobbing) and heat up player
            // We still update Y to keep bobbing, but don't move X/Z closer
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.MoveTowards(currentPos.y, targetPosition.y, moveSpeed * Time.deltaTime);
            transform.position = currentPos;
            
            transform.LookAt(player);
            playerScript.IncreaseHeat(heatDamage * Time.deltaTime);
        }
    }

    // --- NEW: Floor Recovery Logic ---
    // If the drone accidentally hits the floor/wall while NOT frozen, fly up!
    void OnCollisionStay(Collision collision)
    {
        // If we are alive (not frozen) and hitting something (the floor)
        if (!IsFrozen)
        {
            // Force the drone upwards immediately
            Vector3 recoveryPos = transform.position;
            recoveryPos.y += 2.0f * Time.deltaTime; // Lift speed
            transform.position = recoveryPos;
        }
    }

    public void FreezeDrone()
    {
        if (IsFrozen) return;

        IsFrozen = true;
        
        if (rb != null)
        {
            rb.useGravity = true; // Turn ON gravity to fall
            rb.linearDamping = 0.5f; // Reduce drag so it falls faster
        }
        
        // Visual Change
        Renderer droneRenderer = GetComponentInChildren<Renderer>();
        if (droneRenderer != null)
        {
            droneRenderer.material.color = Color.cyan;
        }
        
        Debug.Log("Drone Frozen! Falling to floor...");
    }

    public void SmashDrone()
    {
        Debug.Log("Drone Smashed!");
        Destroy(gameObject);
    }
}