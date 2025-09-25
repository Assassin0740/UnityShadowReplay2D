using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour
{
    private List<Player.PlayerAction> recordedInputs; // Recorded player inputs
    private float actionDuration; // Total duration of recorded actions
    private float playbackTime = 0; // Current playback time
    private Rigidbody2D rb; // Rigidbody2D component reference
    private bool hasFinishedAction = false; // Whether all recorded actions have been executed
    private bool hasLanded = false; // Whether the shadow has landed on the ground

    // Movement parameters inherited from the player
    private float moveSpeed; // Movement speed
    private float jumpForce; // Jump force
    private float groundCheckDistance; // Ground check raycast length
    private LayerMask groundLayer; // Layer mask for ground detection

    public void Initialize(
        List<Player.PlayerAction> inputs,
        float duration,
        float speed,
        float jump,
        float groundCheck,
        LayerMask ground)
    {
        recordedInputs = inputs;
        actionDuration = duration;
        moveSpeed = speed;
        jumpForce = jump;
        groundCheckDistance = groundCheck;
        groundLayer = ground;

        // Ensure the shadow has a Rigidbody2D component
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Set shadow rigidbody properties with gravity scale 2
        rb.gravityScale = 2;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Initialize states
        hasFinishedAction = false;
        hasLanded = false;
    }

    void Update()
    {
        if (recordedInputs == null || recordedInputs.Count == 0 || hasLanded)
            return;

        // Check if landed after finishing actions
        if (hasFinishedAction && IsGrounded())
        {
            hasLanded = true;
            FinishPlayback();
            return;
        }

        // Update playback time
        playbackTime += Time.deltaTime;

        // Mark actions as finished if playback time exceeds recorded duration
        if (playbackTime >= actionDuration && !hasFinishedAction)
        {
            hasFinishedAction = true;
            return;
        }

        // Process inputs if still playing back actions
        if (!hasFinishedAction)
        {
            Player.PlayerAction currentInput = GetCurrentInput();
            if (currentInput.isJumpPressed && IsGrounded())
            {
                Jump();
            }

            // Apply horizontal movement
            Move(currentInput.horizontalInput);
        }
    }

    // Get the input corresponding to the current playback time
    private Player.PlayerAction GetCurrentInput()
    {
        for (int i = 0; i < recordedInputs.Count; i++)
        {
            if (recordedInputs[i].time >= playbackTime)
            {
                return recordedInputs[i];
            }
        }
        return recordedInputs[recordedInputs.Count - 1];
    }

    // Shadow movement logic
    private void Move(float input)
    {
        Vector2 movement = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = movement;

        // Flip shadow facing direction based on input
        if (input != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(input), 1f, 1f);
        }
    }

    // Shadow jump logic
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    // Check if shadow is on the ground
    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    // Destroy shadow after playback finishes
    void FinishPlayback()
    {
        Destroy(gameObject);
    }

    // Draw ground check ray in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
