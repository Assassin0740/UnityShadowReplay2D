using UnityEngine;
using System.Collections.Generic;

// Requires Rigidbody2D component
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Reference Settings")]
    [Tooltip("Jump marker prefab")]
    public GameObject jumpMarkerPrefab;

    [Tooltip("Player shadow prefab")]
    public GameObject shadowPrefab;

    [Tooltip("Collision layer for ground detection")]
    public LayerMask groundLayer;

    [Header("Movement Settings")]
    [Tooltip("Movement speed")]
    public float moveSpeed = 5f;

    [Tooltip("Jump force")] //
    public float jumpForce = 7f;

    [Tooltip(" Ground check raycast length")]
    public float groundCheckDistance = 0.2f;

    private Rigidbody2D rb; // Reference to Rigidbody2D component
    private GameObject jumpMarker; // Jump marker object
    private GameObject currentShadow; // Reference to current shadow
    private bool hasCreatedMarker = false; // Whether marker has been created
    private bool hasCreatedShadow = false; // Whether shadow has been created
    private float markerCreateTime; // Time point when marker was created

    // List used to record player actions
    private List<PlayerAction> actionRecords = new List<PlayerAction>();
    private bool isRecording = false; // Start recording only after marker is created
    private float recordStartTime; // Time point when recording starts

    // Data structure for recording player actions (records input)
    public struct PlayerAction
    {
        public float time; // Relative timestamp
        public float horizontalInput; // Horizontal input (-1, 0, 1)
        public bool isJumpPressed; // Whether jump key is pressed
    }

    void Start()
    {
        // Get Rigidbody2D component
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
         // Record player actions (only after marker is created and before shadow is created)
        if (isRecording && !hasCreatedShadow)
        {
            RecordPlayerAction();
        }

        // Player movement control
        float moveInput = Input.GetAxisRaw("Horizontal");
        Move(moveInput);

        // Jump detection (W key to jump)
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded())
        {
            Jump();
        }

        // S key control: first create marker, then create shadow
        if (Input.GetKeyDown(KeyCode.S))
        {
            // First press S: create marker and start recording actions
            if (!hasCreatedMarker)
            {
                CreateJumpMarker();
                hasCreatedMarker = true;
                markerCreateTime = Time.time;
                // Start recording actions from marker creation to shadow creation
                actionRecords.Clear();
                recordStartTime = Time.time;
                isRecording = true;
            }
            // Second press S: create shadow (if marker exists and shadow not created)
            else if (hasCreatedMarker && !hasCreatedShadow)
            {
                CreatePlayerShadow();
                // Key modification: reset state immediately after shadow creation to allow new marker creation
                hasCreatedMarker = false;
                hasCreatedShadow = false;
                // Destroy marker when shadow is created
                if (jumpMarker != null)
                {
                    Destroy(jumpMarker);
                    jumpMarker = null;
                }
                isRecording = false; // Stop recording
            }
        }
    }

    // Record player input
    void RecordPlayerAction()
    {
        PlayerAction newAction = new PlayerAction();
        newAction.time = Time.time - recordStartTime; // Time relative to recording start
        newAction.horizontalInput = Input.GetAxisRaw("Horizontal"); // Record horizontal input
        newAction.isJumpPressed = Input.GetKeyDown(KeyCode.W); // Record whether jump key is pressed

        actionRecords.Add(newAction);
    }

    // Player movement
    public void Move(float input)
    {
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // Flip player facing direction
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // Player jump (W key)
    public void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    // Create jump marker
    void CreateJumpMarker()
    {
        if (jumpMarkerPrefab != null)
        {
            // Destroy existing marker if it exists
            if (jumpMarker != null)
                Destroy(jumpMarker);

            jumpMarker = Instantiate(jumpMarkerPrefab, transform.position, Quaternion.identity);
            jumpMarker.name = "JumpMarker";
        }
        else
        {
            // Log error: Please assign jump marker prefab
            Debug.LogError("Please assign jump marker prefab!");
        }
    }

    // 创建玩家影子 // Create player shadow
    void CreatePlayerShadow()
    {
        if (shadowPrefab != null && jumpMarker != null)
        {
            currentShadow = Instantiate(shadowPrefab, jumpMarker.transform.position, transform.rotation);
            currentShadow.name = "PlayerShadow_" + Time.time; // Add unique name to each shadow

            // Calculate action duration
            float actionDuration = Time.time - markerCreateTime;

            // Get shadow controller and initialize
            ShadowController shadowController = currentShadow.GetComponent<ShadowController>();
            if (shadowController != null)
            {
                shadowController.Initialize(
                    actionRecords,
                    actionDuration,
                    moveSpeed,
                    jumpForce,
                    groundCheckDistance,
                    groundLayer
                );
            }
            else
            {
                // Log error: ShadowController component not attached to shadow prefab
                Debug.LogError(" ShadowController component not attached to shadow prefab!");
            }

            // Set up shadow collision
            SetupShadowCollision(currentShadow);
        }
        else
        {
            // Log error: Please ensure shadow prefab and jump marker are properly set up
            Debug.LogError("Please ensure shadow prefab and jump marker are properly set up!");
        }
    }

    // Set up shadow collision
    void SetupShadowCollision(GameObject shadow)
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D shadowCollider = shadow.GetComponent<Collider2D>();

        if (playerCollider != null && shadowCollider != null)
        {
            // 不忽略玩家与影子的碰撞 // Do not ignore collision between player and shadow
            Physics2D.IgnoreCollision(playerCollider, shadowCollider, false);
        }
    }

    // Check if on ground
    public bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    // Draw ground check raycast
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}