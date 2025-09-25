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

    // Movement parameters copied from the player to ensure consistent behavior
    private float moveSpeed; // Horizontal movement speed
    private float jumpForce; // Vertical force applied when jumping
    private float groundCheckDistance; // Length of raycast used for ground detection
    private LayerMask groundLayer; // Layer mask to identify what counts as ground

    public void Initialize(
        List<Player.PlayerAction> inputs,
        float duration,
        float speed,
        float jump,
        float groundCheck,
        LayerMask ground,
        LayerMask shadow)
    {
        recordedInputs = inputs;
        actionDuration = duration;
        moveSpeed = speed;
        jumpForce = jump;
        groundCheckDistance = groundCheck;
        groundLayer = ground;

        // Ensure the shadow has a Rigidbody2D component for physics interactions
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Reset all state flags to initial conditions
        hasFinishedAction = false;
        hasLanded = false;
        isJumping = false;
    }

    void Update()
    {
        // Exit early if there's no input data, or the shadow has already landed and finished
        if (recordedInputs == null || recordedInputs.Count == 0 || hasLanded)
            return;

        // Update ground status and jump state before processing actions
        UpdateGroundedState();

        // Destroy shadow once all actions are finished and it has landed
        if (hasFinishedAction && !isJumping)
        {
            hasLanded = true;
            FinishPlayback();
            return;
        }

        // Track time elapsed since starting the action replay
        playbackTime += Time.deltaTime;

        // Mark actions as finished when replay time exceeds recorded duration
        if (playbackTime >= actionDuration && !hasFinishedAction)
        {
            hasFinishedAction = true;
            return;
        }

        // Process recorded inputs while there are actions left to play
        if (!hasFinishedAction)
        {
            Player.PlayerAction currentInput = GetCurrentInput();
            // Only allow jumping if the shadow is on the ground (not mid-air)
            if (currentInput.isJumpPressed && !isJumping)
            {
                Jump();
            }

            // Apply horizontal movement based on recorded input
            Move(currentInput.horizontalInput);
        }
    }

    // Updates whether the shadow is on the ground and manages jump state locking
    private void UpdateGroundedState()
    {
        bool isCurrentlyGrounded = IsGrounded();

        // Unlock jump capability when landing
        if (isCurrentlyGrounded && isJumping)
        {
            isJumping = false;
        }
        // Lock jump capability when leaving the ground
        else if (!isCurrentlyGrounded && !isJumping)
        {
            isJumping = true;
        }
    }

    // Retrieves the recorded input that matches the current playback time
    private Player.PlayerAction GetCurrentInput()
    {
        for (int i = 0; i < recordedInputs.Count; i++)
        {
            if (recordedInputs[i].time >= playbackTime)
            {
                return recordedInputs[i];
            }
        }
        // Return the last input if beyond recorded time range
        return recordedInputs[recordedInputs.Count - 1];
    }

    // Handles horizontal movement using recorded input
    private void Move(float input)
    {
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // Flip sprite direction based on movement input
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // Applies jump force and locks jump state until landing
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true; // Prevent additional jumps until landing
    }

    // Checks if the shadow is touching the ground using a raycast
    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(transform.position, Vector2.down * groundCheckDistance, Color.blue); // Visualize ground check in Scene view
        return hit.collider != null;
    }

    // Destroys the shadow once all actions are complete and it has landed
    void FinishPlayback()
    {
        Destroy(gameObject);
    }

    // Visualizes the ground check ray in the Unity Editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}