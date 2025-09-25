using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour
{
    private List<Player.PlayerAction> recordedInputs; // Stores the sequence of player inputs to replicate
    private float actionDuration; // Total time length of the recorded action sequence
    private float playbackTime = 0; // Elapsed time since starting the action replay
    private Rigidbody2D rb; // Reference to the shadow's Rigidbody2D component for physics
    private bool hasFinishedAction = false; // Tracks if all recorded actions have been processed
    private bool hasLanded = false; // Tracks if the shadow has landed after finishing actions
    private bool isJumping = false; // Tracks if the shadow is in mid-air to prevent consecutive jumps

    // Movement & Ground Check Parameters (copied from Player for consistency)
    private float moveSpeed; // Horizontal movement speed (matches Player)
    private float jumpForce; // Vertical jump force (matches Player)
    private float groundCheckDistance; // Length of ground check rays (matches Player)
    private float diagonalCheckAngle; // Angle of diagonal rays (matches Player)
    private bool enableDualDiagonalCheck; // Dual diagonal check toggle (matches Player)
    private LayerMask groundLayer; // Layer mask for ground detection
    private LayerMask shadowLayer; // Layer mask for shadow detection (to stand on other shadows)

    // Initialize shadow with player's recorded data and parameters
    public void Initialize(
        List<Player.PlayerAction> inputs,
        float duration,
        float speed,
        float jump,
        float groundCheckLen,
        float diagAngle,
        bool enableDualDiag,
        LayerMask ground,
        LayerMask shadow)
    {
        recordedInputs = inputs;
        actionDuration = duration;
        moveSpeed = speed;
        jumpForce = jump;
        groundCheckDistance = groundCheckLen;
        diagonalCheckAngle = diagAngle;
        enableDualDiagonalCheck = enableDualDiag;
        groundLayer = ground;
        shadowLayer = shadow;

        // Ensure shadow has a Rigidbody2D component (add if missing)
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Configure shadow physics (gravity scale = 2 for faster fall than player)
        rb.gravityScale = 2;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Reset all state flags to initial conditions
        hasFinishedAction = false;
        hasLanded = false;
        isJumping = false;
    }

    void Update()
    {
        // Exit early if no input data or shadow has already landed
        if (recordedInputs == null || recordedInputs.Count == 0 || hasLanded)
            return;

        // Update ground status and jump lock before processing actions
        UpdateGroundedState();

        // Destroy shadow once actions are finished AND it has landed
        if (hasFinishedAction && !isJumping)
        {
            hasLanded = true;
            FinishPlayback();
            return;
        }

        // Update playback time (tracks how far into the action sequence we are)
        playbackTime += Time.deltaTime;

        // Mark actions as finished when replay exceeds recorded duration
        if (playbackTime >= actionDuration && !hasFinishedAction)
        {
            hasFinishedAction = true;
            return;
        }

        // Process recorded inputs if still in the action sequence
        if (!hasFinishedAction)
        {
            Player.PlayerAction currentInput = GetCurrentInput();
            // Only allow jumping if shadow is grounded (not mid-air)
            if (currentInput.isJumpPressed && !isJumping)
            {
                Jump();
            }

            // Apply horizontal movement based on recorded input
            Move(currentInput.horizontalInput);
        }
    }

    // Updates shadow's grounded state and manages jump locking
    private void UpdateGroundedState()
    {
        bool isCurrentlyGrounded = IsGrounded();

        // Unlock jump when landing on valid surface
        if (isCurrentlyGrounded && isJumping)
        {
            isJumping = false;
        }
        // Lock jump when leaving the ground
        else if (!isCurrentlyGrounded && !isJumping)
        {
            isJumping = true;
        }
    }

    // Retrieves the recorded input matching the current playback time
    private Player.PlayerAction GetCurrentInput()
    {
        for (int i = 0; i < recordedInputs.Count; i++)
        {
            if (recordedInputs[i].time >= playbackTime)
            {
                return recordedInputs[i];
            }
        }
        // Return last input if playback exceeds recorded time range
        return recordedInputs[recordedInputs.Count - 1];
    }

    // Applies horizontal movement to the shadow
    private void Move(float input)
    {
        // Maintain vertical velocity (gravity/jump) while updating horizontal speed
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // Flip shadow sprite to match movement direction
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // Applies jump force and locks jump state until landing
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true; // Prevent multiple jumps mid-air
    }

    // Checks if shadow is on valid ground OR other shadows (vertical + diagonal rays)
    private bool IsGrounded()
    {
        // Combine ground and shadow layers for unified detection
        LayerMask combinedDetectLayer = groundLayer | shadowLayer;

        // Calculate diagonal ray directions (degrees ¡ú radians)
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

        // Return true if ANY ray hits valid surface
        return verticalHit.collider != null || leftDiagHit.collider != null || rightDiagHit.collider != null;
    }

    // Draws debug rays for shadow's ground check
    private void DrawDebugRays(Vector2 verticalDir, Vector2 leftDiagDir, Vector2 rightDiagDir)
    {
        // Vertical ray (cyan)
        Debug.DrawRay(transform.position, verticalDir * groundCheckDistance, Color.cyan);

        // Diagonal rays (magenta = left, green = right) if enabled
        if (enableDualDiagonalCheck)
        {
            Debug.DrawRay(transform.position, leftDiagDir * groundCheckDistance, Color.magenta);
            Debug.DrawRay(transform.position, rightDiagDir * groundCheckDistance, Color.green);
        }
    }

    // Destroys shadow after action playback and landing
    void FinishPlayback()
    {
        Destroy(gameObject);
    }

    // Draws persistent gizmos for shadow's ground check (edit mode visibility)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) // Avoid overlapping with play-mode debug rays
        {
            float angleInRadians = diagonalCheckAngle * Mathf.Deg2Rad;
            Vector2 leftDiagonalDir = new Vector2(-Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 rightDiagonalDir = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians)).normalized;
            Vector2 verticalDownDir = Vector2.down;

            // Vertical gizmo ray (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)verticalDownDir * groundCheckDistance);

            // Diagonal gizmo rays (magenta = left, green = right) if enabled
            if (enableDualDiagonalCheck)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftDiagonalDir * groundCheckDistance);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightDiagonalDir * groundCheckDistance);
            }
        }
    }
}