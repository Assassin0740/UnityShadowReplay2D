using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Camera controller that smoothly follows the Player using Vector3.Lerp for interpolation
public class Camera : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Assign the Player's Transform here to track it")]
    public Transform playerTarget; // Reference to the Player's transform (assign in Inspector)

    [Tooltip("Fixed offset between camera and Player (adjust for desired view)")]
    public Vector3 offset = new Vector3(0f, 1f, -10f); // Default 2D offset (Z=-10 for orthographic view)

    [Tooltip("Smoothing speed for camera movement (higher = faster follow)")]
    public float smoothingSpeed = 0.125f; // Controls how smooth the follow is


    void Start()
    {
        // Warn if Player target isn't assigned
        if (playerTarget == null)
        {
            Debug.LogError("Player Target not assigned! Please drag the Player's Transform into the Camera script's 'Player Target' field.");
        }
    }


    // Use LateUpdate to ensure camera follows AFTER Player movement (prevents jitter)
    void LateUpdate()
    {
        // Only run if Player target exists
        if (playerTarget != null)
        {
            // Calculate the target position (Player position + fixed offset)
            Vector3 targetPosition = playerTarget.position + offset;

            // Smoothly interpolate from current camera position to target position
            // Time.deltaTime ensures consistent smoothing across different frame rates
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothingSpeed * Time.deltaTime);

            // Update camera position to the smoothed position
            transform.position = smoothedPosition;
        }
    }
}