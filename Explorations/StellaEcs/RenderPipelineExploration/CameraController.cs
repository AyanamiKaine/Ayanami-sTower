using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Input;

namespace RenderPipelineExploration;

/// <summary>
/// FPS-style camera controller with smoothing and mouse-look.
/// Keeps logic out of the Game loop and drives a Camera instance.
/// </summary>
public sealed class CameraController
{
    private readonly Camera _camera;

    // State
    // Smoothed velocity in world space (units/sec)
    private Vector3 _worldVelocity = Vector3.Zero;
    private bool _relativeMouseEnabled = false;

    // Tunables
    /// <summary>
    /// Maximum movement speed of the camera (units/sec).
    /// </summary>
    public float MaxSpeed { get; set; } = 6.0f;          // walk speed (units/sec)
    /// <summary>
    /// Acceleration towards the desired velocity (units/sec^2).
    /// </summary>
    public float Acceleration { get; set; } = 12.0f;     // accel towards desired vel
    /// <summary>
    /// Multiplier applied while sprinting (LeftShift held).
    /// </summary>
    public float SprintMultiplier { get; set; } = 1.8f;  // while LeftShift held
    /// <summary>
    /// Mouse sensitivity for look controls (radians per mouse pixel).
    /// </summary>
    public float MouseSensitivity { get; set; } = 0.0025f; // radians per mouse pixel
    /// <summary>
    /// Rotation speed for arrow key look controls (radians/sec).
    /// </summary>
    public float ArrowRotSpeed { get; set; } = 1.5f;     // radians/sec for arrow key look
    /// <summary>
    /// Camera controller for managing camera movement and rotation.
    /// </summary>
    public CameraController(Camera camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Update camera from input. Hold RMB for mouse-look. WASD + Space/Ctrl to move. Shift to sprint.
    /// </summary>
    public void Update(Inputs inputs, Window window, TimeSpan delta)
    {
        float dt = (float)delta.TotalSeconds;
        var kb = inputs.Keyboard;
        var mouse = inputs.Mouse;

        // Toggle relative mouse mode while holding RMB
        bool wantRelative = mouse.RightButton.IsDown;
        if (wantRelative != _relativeMouseEnabled)
        {
            _relativeMouseEnabled = wantRelative;
            window.SetRelativeMouseMode(_relativeMouseEnabled);
            if (_relativeMouseEnabled) mouse.Hide(); else mouse.Show();
        }

        // Mouse look when enabled
        if (_relativeMouseEnabled)
        {
            var dx = mouse.DeltaX;
            var dy = mouse.DeltaY;
            if (dx != 0 || dy != 0)
            {
                _camera.Rotate(dx * MouseSensitivity, -dy * MouseSensitivity);
            }
        }

        // Arrow key look (optional fallback)
        float yaw = 0f;
        if (kb.IsDown(KeyCode.Right)) yaw += 1f;
        if (kb.IsDown(KeyCode.Left)) yaw -= 1f;
        float pitch = 0f;
        if (kb.IsDown(KeyCode.Up)) pitch += 1f;
        if (kb.IsDown(KeyCode.Down)) pitch -= 1f;
        if (yaw != 0f || pitch != 0f)
        {
            _camera.Rotate(yaw * ArrowRotSpeed * dt, pitch * ArrowRotSpeed * dt);
        }

        // Movement (WASD + Space/Ctrl) with smoothing
        float forward = 0f;
        if (kb.IsDown(KeyCode.W)) forward += 1f;
        if (kb.IsDown(KeyCode.S)) forward -= 1f;

        float right = 0f;
        if (kb.IsDown(KeyCode.D)) right += 1f;
        if (kb.IsDown(KeyCode.A)) right -= 1f;

        float up = 0f;
        if (kb.IsDown(KeyCode.Space)) up += 1f;
        if (kb.IsDown(KeyCode.LeftControl)) up -= 1f;

        // Normalize to avoid faster diagonals
        var inputMag = MathF.Sqrt((forward * forward) + (right * right) + (up * up));
        if (inputMag > 1f)
        {
            forward /= inputMag; right /= inputMag; up /= inputMag;
        }

        float maxSpeed = MaxSpeed * (kb.IsHeld(KeyCode.LeftShift) ? SprintMultiplier : 1.0f);
        // Compute desired velocity in world space based on current camera basis
        var desiredWorldVel = ((_camera.Forward * forward) + (_camera.Right * right) + (Vector3.UnitY * up)) * maxSpeed;

        // Exponential smoothing towards desired velocity (in world space)
        float smoothing = 1f - MathF.Exp(-Acceleration * dt);
        _worldVelocity = Vector3.Lerp(_worldVelocity, desiredWorldVel, smoothing);

        if (_worldVelocity != Vector3.Zero)
        {
            _camera.Move(_worldVelocity * dt);
        }
    }
}
