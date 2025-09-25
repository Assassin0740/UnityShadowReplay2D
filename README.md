# Shadow Echo
A lightweight 2D Unity demo featuring **player action replication via shadow clones**—where shadows mirror your exact inputs (not just position) and enable interactive physics like jumping on shadows.


## Overview
Shadow Echo is a 2D platformer-style demo that showcases core mechanics for input-driven shadow replication. Unlike traditional position-interpolated clones, this demo records your raw inputs (movement/jumps) and replays them through shadows with independent physics. Key features include seamless shadow spawning, natural landing-based shadow destruction, and interactive collision between the player and shadows.


## Key Features
- **Input-Driven Shadow Replication**: Shadows replay your exact keyboard inputs (WASD) instead of just interpolating positions—ensuring authentic movement matching.
- **Continuous Shadow Spawning**: Spawn new shadows immediately after creating one (no wait for previous shadows to disappear).
- **Physics Independence**: Shadows have unique physics (e.g., gravity scale = 2) while inheriting your movement speed/jump force.
- **Landing-Based Destruction**: Shadows only disappear after finishing your recorded actions *and* landing on the ground.
- **Shadow Interaction**: Players can stand on and jump from shadows (full collision support).
- **Smooth Camera Follow**: Camera uses `Vector3.Lerp` to smoothly track the player without jitter.


## Quick Start
### Prerequisites
- Unity 2021.3 LTS or later (compatible with most 2D-focused Unity versions).
- Git (to clone the repository).

### Setup Steps
1. **Clone the Repository**  
   ```bash
   git clone https://github.com/your-username/shadow-echo.git
   cd shadow-echo
   ```

2. **Open the Project**  
   Launch Unity, select "Open Project," and navigate to the cloned `shadow-echo` folder.

3. **Configure Layers**  
   The demo relies on two critical layers (create them in `Edit > Project Settings > Tags and Layers` if missing):
   - `Ground`: Assign to all ground/platform objects (for ground detection).
   - `Shadow`: Assign to the `Shadow` prefab (for shadow collision/interaction).

4. **Validate Prefab References**  
   Open your main scene (e.g., `Scenes/ShadowEchoDemo.unity`), select the `Player` GameObject, and verify these in the Inspector:
   - `Jump Marker Prefab`: Assign `Prefabs/JumpMarker.prefab`.
   - `Shadow Prefab`: Assign `Prefabs/Shadow.prefab`.
   - `Ground Layer`: Select the `Ground` layer.
   - `Shadow Layer`: Select the `Shadow` layer.

5. **Run the Demo**  
   Click the "Play" button in Unity—you’re ready to control the player and spawn shadows!


## Controls
| Key   | Action                                                                 |
|-------|-----------------------------------------------------------------------|
| A/D   | Move left/right                                                       |
| W     | Jump (only when grounded on `Ground` or `Shadow` layers)              |
| S     | 1st Press: Spawn a marker and start recording your actions.<br>2nd Press: Spawn a shadow at the marker (destroys the marker). |


## Technical Details
### Core Mechanics
#### 1. Input Recording (Player Script)
The `Player` script records your inputs as `PlayerAction` structs (timestamp, horizontal input, jump state) whenever a marker is active. This ensures shadows replicate *how* you moved, not just *where* you went.

```csharp
public struct PlayerAction
{
    public float time;          // Relative time since recording started
    public float horizontalInput; // -1 (left), 0 (none), 1 (right)
    public bool isJumpPressed;  // True if W was pressed this frame
}
```

#### 2. Shadow Replication (ShadowController Script)
Shadows use the recorded `PlayerAction` list to replay inputs:
- `Initialize()`: Inherits player movement parameters (speed, jump force) and layer masks.
- `Update()`: Matches the current playback time to the recorded input frame and applies movement/jumps.
- `IsGrounded()`: Detects collision with both `Ground` and `Shadow` layers (using bitwise OR: `groundLayer | shadowLayer`).

#### 3. Physics & Collision
- **Shadow Physics**: Shadows have `gravityScale = 2` (faster fall than the player) for visual distinction.
- **Collision Handling**: `SetupShadowCollision()` ensures the player and shadow don’t pass through each other (`Physics2D.IgnoreCollision = false`).


### Script Structure
| Script               | Purpose                                                                 |
|----------------------|-------------------------------------------------------------------------|
| `Player.cs`          | Manages player movement, input recording, marker/shadow spawning.      |
| `ShadowController.cs`| Replays recorded inputs for shadows, handles shadow physics/destruction.|
| `CameraFollow.cs`    | Smoothly tracks the player using `Vector3.Lerp` (runs in `LateUpdate` to avoid jitter). |


## Troubleshooting
| Issue                                  | Solution                                                                 |
|----------------------------------------|--------------------------------------------------------------------------|
| Shadow won’t jump                      | 1. Verify the shadow’s `IsGrounded()` ray hits `Ground`/`Shadow`.<br>2. Check if `jumpForce` is assigned in `Player` settings. |
| Player can’t stand on shadows          | 1. Ensure the shadow’s layer is set to `Shadow`.<br>2. Confirm `SetupShadowCollision()` isn’t ignoring player/shadow collisions. |
| Camera is jittery                      | 1. Ensure `CameraFollow` uses `LateUpdate()` (not `Update()`).<br>2. Adjust `smoothingSpeed` (0.125f is a good default). |
| Marker/shadow doesn’t spawn            | 1. Check if `jumpMarkerPrefab`/`shadowPrefab` are assigned in `Player`.<br>2. Verify no errors in the Console (Window > Console). |


## License
This project is licensed under the MIT License—see the [LICENSE](LICENSE) file for details.


## Contributing
Feel free to fork the repository, submit issues, or open pull requests! Ideas for extensions:
- Add shadow color customization.
- Implement a "shadow limit" (e.g., max 3 shadows at once).
- Add puzzle mechanics (e.g., shadows trigger switches).
