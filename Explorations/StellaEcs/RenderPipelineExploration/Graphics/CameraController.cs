using System;
using System.Numerics;
using AyanamisTower.StellaEcs.HighPrecisionMath;
using MoonWorks;
using MoonWorks.Input;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// FPS-style camera controller with mouse-look and immediate movement (no smoothing).
/// </summary>
public sealed class CameraController
{
    private readonly Camera _camera;
    private bool _relativeMouseEnabled = false;

    // Tunables
    /// <summary>
    /// Maximum camera movement speed in units per second.
    /// </summary>
    public double MaxSpeed { get; set; } = 6.0f;          // units/sec
    /// <summary>
    /// Multiplier applied to <see cref="MaxSpeed"/> while LeftShift is held.
    /// </summary>
    public double SprintMultiplier { get; set; } = 100.8f;  // hold LeftShift
    /// <summary>
    /// Mouse-look sensitivity in radians per mouse pixel.
    /// </summary>
    public double MouseSensitivity { get; set; } = 0.0015f; // radians per mouse pixel
    /// <summary>
    /// Arrow-key look rotation speed in radians per second.
    /// </summary>
    public double ArrowRotSpeed { get; set; } = 1.5f;     // radians/sec for arrow key look

    /// <summary>
    /// Creates a new camera controller that drives the given camera.
    /// </summary>
    /// <param name="camera">The camera instance to control.</param>
    public CameraController(Camera camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Update camera from input. Hold RMB for mouse-look. WASD + Space/Ctrl to move. Shift to sprint.
    /// </summary>
    public void Update(Inputs inputs, Window window, TimeSpan delta)
    {
        double dt = delta.TotalSeconds;
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
            var mouseDelta = new Vector2Double(inputs.Mouse.DeltaX, inputs.Mouse.DeltaY);
            if (mouseDelta != Vector2Double.Zero)
            {
                // Positive yaw when moving mouse right; keep pitch inverted for natural feel
                _camera.Rotate(mouseDelta.X * MouseSensitivity, -mouseDelta.Y * MouseSensitivity);
            }

        }

        // Arrow key look (optional fallback)
        double yaw = 0f;
        if (kb.IsDown(KeyCode.Right)) yaw += 1f;
        if (kb.IsDown(KeyCode.Left)) yaw -= 1f;
        double pitch = 0f;
        if (kb.IsDown(KeyCode.Up)) pitch += 1f;
        if (kb.IsDown(KeyCode.Down)) pitch -= 1f;
        if (yaw != 0f || pitch != 0f)
        {
            _camera.Rotate(yaw * ArrowRotSpeed * dt, pitch * ArrowRotSpeed * dt);
        }

        // Movement (WASD + Space/Ctrl) immediate (no smoothing)
        double forward = 0f;
        if (kb.IsDown(KeyCode.W)) forward += 1f;
        if (kb.IsDown(KeyCode.S)) forward -= 1f;

        double right = 0f;
        if (kb.IsDown(KeyCode.D)) right += 1f;
        if (kb.IsDown(KeyCode.A)) right -= 1f;

        double up = 0f;
        if (kb.IsDown(KeyCode.Space)) up += 1f;
        if (kb.IsDown(KeyCode.LeftControl)) up -= 1f;

        // Normalize to avoid faster diagonals
        double inputMag = Math.Sqrt((forward * forward) + (right * right) + (up * up));
        if (inputMag > 1f)
        {
            forward /= inputMag; right /= inputMag; up /= inputMag;
        }

        double speed = MaxSpeed * (kb.IsHeld(KeyCode.LeftShift) ? SprintMultiplier : 1.0);

        // World-space velocity based on current camera basis
        Vector3Double worldVel = ((_camera.Forward * forward) + (_camera.Right * right) + (Vector3Double.UnitY * up)) * speed;
        if (worldVel != Vector3Double.Zero)
        {
            _camera.Move(worldVel * dt);
        }
    }
}
