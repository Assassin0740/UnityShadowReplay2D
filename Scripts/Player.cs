using UnityEngine;
using System.Collections.Generic;

// Requires Rigidbody2D component to enable physics-based movement
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Reference Settings")]
    [Tooltip("Prefab for the visual jump marker")]
    public GameObject jumpMarkerPrefab;

    [Tooltip("Prefab for the player's shadow clone")]
    public GameObject shadowPrefab;

    [Tooltip("Layer mask for detecting ground surfaces")]
    public LayerMask groundLayer;

    [Tooltip("Layer mask for detecting shadow clones (to jump on shadows)")]
    public LayerMask shadowLayer;

    [Tooltip("Layer mask for detecting shadow clones (to jump on players)")]
    public LayerMask playerLayer;

    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed (units per second)")]
    public float moveSpeed = 5f;

    [Tooltip("Vertical force applied when jumping")]
    public float jumpForce = 7f;

    [Header("Ground Check Settings")]
    [Tooltip("Length of all ground check rays (vertical + diagonal)")]
    public float groundCheckDistance = 0.2f;

    [Tooltip("Angle of diagonal ground check rays (relative to straight down, in degrees)")]
    public float diagonalCheckAngle = 30f;

    [Tooltip("Enable dual diagonal checks (left + right) for better slope/edge detection")]
    public bool enableDualDiagonalCheck = true;

    private Rigidbody2D rb; // Reference to the player's Rigidbody2D component
    private GameObject jumpMarker; // Active jump marker instance
    private GameObject currentShadow; // Reference to the most recently spawned shadow
    private bool hasCreatedMarker = false; // Tracks if a jump marker exists
    private bool hasCreatedShadow = false; // Tracks if a shadow is active
    private float markerCreateTime; // Timestamp when the marker was spawned

    // List to store recorded player inputs for shadow replication
    private List<PlayerAction> actionRecords = new List<PlayerAction>();
    private bool isRecording = false; // Toggles input recording (starts after marker spawn)
    private float recordStartTime; // Timestamp when input recording began

    // Data structure to store individual player input frames
    public struct PlayerAction
    {
        public float time; // Time since recording started (relative timestamp)
        public float horizontalInput; // Horizontal input value (-1 = left, 0 = none, 1 = right)
        public bool isJumpPressed; // Whether the jump key was pressed this frame
    }

    void Start()
    {
        // Get and cache the Rigidbody2D component on start
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Record inputs only if recording is active and no shadow has been spawned yet
        if (isRecording && !hasCreatedShadow)
        {
            RecordPlayerAction();
        }

        // Handle horizontal movement input
        float moveInput = Input.GetAxisRaw("Horizontal");
        Move(moveInput);

        // Handle jump input (only if player is on ground or shadow)
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded())
        {
            Jump();
        }

        // Handle shadow marker system with S key
        if (Input.GetKeyDown(KeyCode.S))
        {
            // First S press: Spawn marker and start recording inputs
            if (!hasCreatedMarker)
            {
                CreateJumpMarker();
                hasCreatedMarker = true;
                markerCreateTime = Time.time;
                // Reset recording state and start fresh
                actionRecords.Clear();
                recordStartTime = Time.time;
                isRecording = true;
            }
            // Second S press: Spawn shadow from recorded inputs and reset marker state
            else if (hasCreatedMarker && !hasCreatedShadow)
            {
                CreatePlayerShadow();
                // Reset marker/shadow flags to allow spawning new markers immediately
                hasCreatedMarker = false;
                hasCreatedShadow = false;
                // Destroy marker once shadow is spawned
                if (jumpMarker != null)
                {
                    Destroy(jumpMarker);
                    jumpMarker = null;
                }
                isRecording = false; // Stop input recording
            }
        }
    }

    // Records the current input state as a PlayerAction and adds it to the action list
    void RecordPlayerAction()
    {
        PlayerAction newAction = new PlayerAction();
        newAction.time = Time.time - recordStartTime; // Use relative time for consistent replay
        newAction.horizontalInput = Input.GetAxisRaw("Horizontal");
        newAction.isJumpPressed = Input.GetKeyDown(KeyCode.W);

        actionRecords.Add(newAction);
    }

    // Applies horizontal movement based on input
    public void Move(float input)
    {
        // Maintain vertical velocity (from gravity/jump) while updating horizontal velocity
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // Flip player sprite to face movement direction (if moving left/right)
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // Applies vertical jump force to the player
    public void Jump()
    {
        // Keep horizontal velocity intact while setting vertical jump force
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    // Spawns a visual marker at the player's current position
    void CreateJumpMarker()
    {
        if (jumpMarkerPrefab != null)
        {
            // Destroy existing marker first to prevent duplicates
            if (jumpMarker != null)
                Destroy(jumpMarker);

            // Spawn new marker at player position with default rotation
            jumpMarker = Instantiate(jumpMarkerPrefab, transform.position, Quaternion.identity);
            jumpMarker.name = "JumpMarker";
        }
        else
        {
            Debug.LogError("Jump marker prefab is not assigned! Assign it in the Player inspector.");
        }
    }

    // Spawns a shadow clone that replicates recorded player actions
    void CreatePlayerShadow()
    {
        if (shadowPrefab != null && jumpMarker != null)
        {
            // Spawn shadow at the marker's position, matching player's rotation
            currentShadow = Instantiate(shadowPrefab, jumpMarker.transform.position, transform.rotation);
            currentShadow.name = "PlayerShadow_" + Time.time; // Unique name for debugging

            // Calculate total time of the recorded action sequence
            float actionDuration = Time.time - markerCreateTime;

            // Get the ShadowController component and initialize it with recorded data
            ShadowController shadowController = currentShadow.GetComponent<ShadowController>();
            if (shadowController != null)
            {
                shadowController.Initialize(
                    actionRecords,
                    actionDuration,
                    moveSpeed,
                    jumpForce,
                    groundCheckDistance,
                    diagonalCheckAngle,
                    enableDualDiagonalCheck,
                    groundLayer,
                    shadowLayer,
                    playerLayer
                );
            }
            else
            {
                Debug.LogError("Shadow prefab is missing the ShadowController component!");
            }

            // Configure collision settings between player and shadow
            SetupShadowCollision(currentShadow);
        }
        else
        {
            Debug.LogError("Shadow prefab or jump marker is missing! Ensure both are set up.");
        }
    }

    // Configures collision between player and shadow (prevents passing through each other)
    void SetupShadowCollision(GameObject shadow)
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D shadowCollider = shadow.GetComponent<Collider2D>();

        if (playerCollider != null && shadowCollider != null)
        {
            // Do NOT ignore collision between player and shadow (allows player to stand on shadow)
            Physics2D.IgnoreCollision(playerCollider, shadowCollider, false);
        }
    }

    // Checks if the player is standing on valid ground OR shadow (vertical + diagonal rays)
    public bool IsGrounded()
    {
        // Combine ground and shadow layers for unified detection
        LayerMask combinedDetectLayer = groundLayer | shadowLayer;

        // Calculate diagonal ray directions (convert degrees to radians first)
        float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
        Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
        Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
        Vector2 verticalDownDir = Vector2.down;

        // Cast rays and check for collisions
        RaycastHit2D verticalHit = Physics2D.Raycast(transform.position, verticalDownDir, groundCheckDistance, combinedDetectLayer);
        RaycastHit2D leftDiagHit = enableDualDiagonalCheck ? Physics2D.Raycast(transform.position, leftDiagonalDir, groundCheckDistance, combinedDetectLayer) : default;
        RaycastHit2D rightDiagHit = enableDualDiagonalCheck ? Physics2D.Raycast(transform.position, rightDiagonalDir, groundCheckDistance, combinedDetectLayer) : default;

        // Draw debug rays (visible in Scene view during play mode)
        DrawDebugRays(verticalDownDir, leftDiagonalDir, rightDiagonalDir);

        // Return true if ANY ray hits valid ground/shadow
        return verticalHit.collider != null || leftDiagHit.collider != null || rightDiagHit.collider != null;
    }

    // Draws debug rays for ground check visualization
    private void DrawDebugRays(Vector2 verticalDir, Vector2 leftDiagDir, Vector2 rightDiagDir)
    {
        // Vertical ray (red)
        Debug.DrawRay(transform.position, verticalDir * groundCheckDistance, Color.red);

        // Diagonal rays (yellow = left, blue = right) if enabled
        if (enableDualDiagonalCheck)
        {
            Debug.DrawRay(transform.position, leftDiagDir * groundCheckDistance, Color.yellow);
            Debug.DrawRay(transform.position, rightDiagDir * groundCheckDistance, Color.blue);
        }
    }

    // Draws persistent gizmos (visible in Scene view even in edit mode)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) // Avoid overlapping with play-mode debug rays
        {
            LayerMask combinedDetectLayer = groundLayer | shadowLayer;
            float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
            Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 verticalDownDir = Vector2.down;

            // Vertical gizmo ray (red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)verticalDownDir * groundCheckDistance);

            // Diagonal gizmo rays (yellow = left, blue = right) if enabled
            if (enableDualDiagonalCheck)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftDiagonalDir * groundCheckDistance);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightDiagonalDir * groundCheckDistance);
            }
        }
    }
}