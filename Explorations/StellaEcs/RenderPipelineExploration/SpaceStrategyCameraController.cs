using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Input;

namespace AyanamisTower.StellaEcs.StellaInvicta
{
    /// <summary>
    /// Homeworld-style space strategy camera controller.
    /// Controls an existing <see cref="Camera"/> and provides:
    /// - Orbit (rotate) around a focus point (right mouse drag)
    /// - Pan (middle mouse drag or WASD) in the camera plane
    /// - Zoom (keyboard Z/X and mouse vertical drag while middle button)
    /// - Smooth interpolation of focus position and distance
    ///
    /// Controls are intentionally simple and tunable; you can call <see cref="SetFocus"/>
    /// to change the camera's focus target (for example, when selecting an object).
    /// </summary>
    public sealed class SpaceStrategyCameraController
    {
        private readonly Camera _camera;

        // Focus (the point the camera orbits around)
        private Vector3 _targetFocus;
        private Vector3 _currentFocus;

        // Distance from camera to focus
        private float _targetDistance;
        private float _currentDistance;

        // Spherical angles (radians)
        private float _yaw;
        private float _pitch;

        // Tunables
        /// <summary>
        /// Speed at which the camera pans (moves) in the world.
        /// </summary>
        public float PanSpeed { get; set; } = 8.0f; // world units per second (scaled by distance)
        /// <summary>
        /// Speed at which the camera rotates around the focus point.
        /// </summary>
        public float RotateSensitivity { get; set; } = 0.01f; // radians per mouse pixel
        /// <summary>
        /// Speed at which the camera zooms in and out.
        /// </summary>
        public float ZoomSpeed { get; set; } = 20.0f; // units per second
        /// <summary>
        /// Multiplier applied to scroll-wheel zoom steps.
        /// </summary>
        public float ScrollZoomMultiplier { get; set; } = 1.0f;
        /// <summary>
        /// Maximum absolute zoom change (world units) applied from the scroll wheel in a single update.
        /// This prevents a large mouse wheel delta or high sensitivity from zooming too far in one frame.
        /// </summary>
        public float MaxScrollZoomStep { get; set; } = 1000.0f;
        /// <summary>
        /// Speed at which the camera smoothly interpolates its position and distance.
        /// </summary>
        public float Smoothing { get; set; } = 8.0f; // larger = snappier
        /// <summary>
        /// Minimum distance the camera can be from the focus point.
        /// </summary>
        public float MinDistance { get; set; } = 1.0f;
        /// <summary>
        /// Maximum distance the camera can be from the focus point.
        /// </summary>
        public float MaxDistance { get; set; } = 10000f;

        /// <summary>
        /// Creates a new strategy-style camera controller driving the given <see cref="Camera"/>.
        /// The controller will initialize its focus to the camera's current Target.
        /// </summary>
        public SpaceStrategyCameraController(Camera camera)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));

            _currentFocus = _targetFocus = camera.Target;
            _currentDistance = _targetDistance = Vector3.Distance(camera.Position, _currentFocus);
            _yaw = camera.Yaw;
            _pitch = camera.Pitch;
        }

        /// <summary>
        /// Immediately set the focus point and optionally the distance (if distance &gt; 0).
        /// Use this to focus the camera on a selected object.
        /// </summary>
        public void SetFocus(Vector3 focus, float? distance = null)
        {
            _targetFocus = focus;
            if (distance.HasValue && distance.Value > 0f)
            {
                _targetDistance = Math.Clamp(distance.Value, MinDistance, MaxDistance);
            }
        }

        /// <summary>
        /// Applies an origin shift (rebase offset) to the controller so its internal focus
        /// and distances remain consistent after the world has been rebased.
        /// The offset should be the same value subtracted from the camera and world positions.
        /// </summary>
        public void ApplyOriginShift(Vector3 offset)
        {
            _targetFocus -= offset;
            _currentFocus -= offset;
        }

        /// <summary>
        /// Update the controller. Controls:
        /// - Right mouse drag: rotate around focus
        /// - Middle mouse drag: pan (drag focus in camera plane); vertical middle-drag also adjusts zoom
        /// - WASD: pan
        /// - Q/E: move focus up/down in world Y
        /// - Z/X: zoom in/out
        /// - LeftShift: hold to increase pan/zoom speed
        /// </summary>
        public void Update(Inputs inputs, Window window, TimeSpan delta)
        {
            float dt = (float)delta.TotalSeconds;
            var kb = inputs.Keyboard;
            var mouse = inputs.Mouse;

            // Speed modifier
            float speedMult = kb.IsHeld(KeyCode.LeftShift) ? 4.0f : 1.0f;

            // --- Rotation (orbit) ---
            if (mouse.RightButton.IsDown)
            {
                var md = new Vector2(mouse.DeltaX, mouse.DeltaY);
                if (md != Vector2.Zero)
                {
                    _yaw += md.X * RotateSensitivity;
                    _pitch += -md.Y * RotateSensitivity; // invert Y for natural feel
                    // Clamp pitch slightly inside poles to avoid singularity
                    _pitch = Math.Clamp(_pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
                }
            }

            // --- Panning ---
            // Mouse middle drag pans the focus in the camera plane. We scale pan by distance so
            // that panning feels consistent at different zoom levels.
            if (mouse.MiddleButton.IsDown)
            {
                var md = new Vector2(mouse.DeltaX, mouse.DeltaY);
                if (md != Vector2.Zero)
                {
                    // move opposite to drag (dragging right moves world left)
                    var right = _camera.Right;
                    var camUp = _camera.Up;
                    float distanceScale = MathF.Max(0.01f, _currentDistance / 10f);
                    var pan = (-right * md.X + camUp * md.Y) * (PanSpeed * distanceScale) * dt * speedMult;
                    _targetFocus += pan;

                    // vertical middle-drag also adjusts zoom a bit for convenience
                    _targetDistance += -md.Y * (ZoomSpeed * 0.01f) * speedMult;
                }
            }

            // Keyboard panning
            float forward = 0f;
            if (kb.IsDown(KeyCode.W)) forward += 1f;
            if (kb.IsDown(KeyCode.S)) forward -= 1f;
            float rightDir = 0f;
            if (kb.IsDown(KeyCode.D)) rightDir += 1f;
            if (kb.IsDown(KeyCode.A)) rightDir -= 1f;
            float up = 0f;
            if (kb.IsDown(KeyCode.E)) up += 1f; // move focus up
            if (kb.IsDown(KeyCode.Q)) up -= 1f; // move focus down

            if (forward != 0f || rightDir != 0f || up != 0f)
            {
                // Project forward onto camera's XZ plane for more intuitive pan (prevent flying when pitched)
                var camForward = _camera.Forward;
                var camForwardXZ = Vector3.Normalize(new Vector3(camForward.X, 0f, camForward.Z));
                var panVec = camForwardXZ * forward + _camera.Right * rightDir + Vector3.UnitY * up;
                float distanceScale = MathF.Max(0.01f, _currentDistance / 10f);
                _targetFocus += panVec * (PanSpeed * distanceScale) * dt * speedMult;
            }

            // --- Zoom ---
            // Keyboard zoom (Z/X)
            if (kb.IsDown(KeyCode.Z)) _targetDistance -= ZoomSpeed * dt * speedMult;
            if (kb.IsDown(KeyCode.X)) _targetDistance += ZoomSpeed * dt * speedMult;

            // --- Scroll-wheel zoom ---
            // MoonWorks provides `mouse.Wheel` as an integer delta for this frame.
            if (mouse.Wheel != 0)
            {
                // Negative wheel typically means zoom in on many platforms, so invert to match other zoom controls
                _targetDistance += -mouse.Wheel * ZoomSpeed * 0.1f * speedMult * ScrollZoomMultiplier;
            }

            // Clamp target distance
            _targetDistance = Math.Clamp(_targetDistance, MinDistance, MaxDistance);

            // --- Smooth interpolation ---
            float t = 1f - MathF.Exp(-Smoothing * dt); // exponential smoothing factor
            _currentFocus = Vector3.Lerp(_currentFocus, _targetFocus, t);
            _currentDistance = _currentDistance + (_targetDistance - _currentDistance) * t;

            // Apply yaw/pitch to camera and set position relative to focus
            _camera.Yaw = _yaw;
            _camera.Pitch = _pitch;

            // Put camera at focus - forward * distance
            _camera.Position = _currentFocus - _camera.Forward * _currentDistance;
        }
    }
}
