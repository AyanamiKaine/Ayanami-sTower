using System;
using System.Numerics;
using AyanamisTower.StellaEcs.HighPrecisionMath;
using MoonWorks;
using MoonWorks.Input;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

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

    // Track whether we've put the window into SDL relative-mouse mode
    private bool _isRelativeMouseMode = false;

    // Focus (the point the camera orbits around)
    private Vector3Double _targetFocus;
    private Vector3Double _currentFocus;
    // Optional provider that returns the live focus point each frame (useful to follow moving objects)
    private Func<Vector3Double>? _focusProvider;
    // When a live provider is set we may want to smooth the initial transition
    // so selecting a new object doesn't teleport the camera. This timer tracks
    // remaining seconds to apply smoothing while beginning to follow a provider.
    private double _followSmoothingRemaining = 0f;
    // Total smoothing duration originally requested (used to blend rates smoothly)
    private double _followSmoothingTotal = 0f;
    // Optional key used to identify the currently-followed object so repeated
    // calls to follow the same object can be ignored.
    private object? _currentProviderKey;

    // Distance from camera to focus
    private double _targetDistance;
    private double _currentDistance;
    // Optional per-focus minimum distance override to prevent camera clipping into focused objects.
    private double? _focusMinDistanceOverride;

    // Spherical angles (radians)
    private double _yaw;
    private double _pitch;
    // Targets for smooth rotation and tuning
    private double _targetYaw;
    private double _targetPitch;
    // When true, the next Update will snap current focus/distance to target values (no smoothing)

    // Tunables
    /// <summary>
    /// Speed at which the camera pans (moves) in the world.
    /// </summary>
    public double PanSpeed { get; set; } = 8.0f; // world units per second (scaled by distance)
    /// <summary>
    /// Speed at which the camera rotates around the focus point.
    /// </summary>
    public double RotateSensitivity { get; set; } = 0.01f; // radians per mouse pixel
    /// <summary>
    /// Enable/disable smooth interpolation of rotation (yaw/pitch).
    /// </summary>
    public bool SmoothRotation { get; set; } = true;
    /// <summary>
    /// Speed at which yaw/pitch interpolate to their target values. Larger = snappier.
    /// </summary>
    public double RotationSmoothing { get; set; } = 3.0f;
    /// <summary>
    /// Speed at which the camera zooms in and out.
    /// </summary>
    public double ZoomSpeed { get; set; } = 20.0f; // units per second
    /// <summary>
    /// Multiplier applied to scroll-wheel zoom steps.
    /// </summary>
    public double ScrollZoomMultiplier { get; set; } = 1.0f;
    /// <summary>
    /// Maximum absolute zoom change (world units) applied from the scroll wheel in a single update.
    /// This prevents a large mouse wheel delta or high sensitivity from zooming too far in one frame.
    /// </summary>
    public double MaxScrollZoomStep { get; set; } = 1000.0f;
    /// <summary>
    /// Speed at which the camera smoothly interpolates its position and distance.
    /// </summary>
    public double Smoothing { get; set; } = 8.0f; // larger = snappier
    /// <summary>
    /// Minimum distance the camera can be from the focus point.
    /// </summary>
    public double MinDistance { get; set; } = 0.1f;
    /// <summary>
    /// Maximum distance the camera can be from the focus point.
    /// </summary>
    public double MaxDistance { get; set; } = 10000f;
    /// <summary>
    /// Enable/disable RTS-style edge panning (move mouse to screen edges to pan camera).
    /// </summary>
    public bool EdgePanEnabled { get; set; } = true;
    /// <summary>
    /// Thickness in pixels of the screen edge hot zones for edge panning.
    /// </summary>
    public int EdgePanThreshold { get; set; } = 12;
    /// <summary>
    /// Edge pan speed in world units per second (scaled by distance). Defaults to PanSpeed.
    /// </summary>
    public double EdgePanSpeed { get; set; } = 8.0f;

    /// <summary>
    /// Exponent used when scaling pan speed by distance. The controller computes a
    /// distance scale from _currentDistance/10 and raises it to this exponent.
    /// Use values &lt;1 to reduce growth when zoomed out (default 0.5 = sqrt).
    /// </summary>
    public double PanDistanceScaleExponent { get; set; } = 0.5f;

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
        // Initialize rotation targets to the camera's current angles
        _targetYaw = _yaw;
        _targetPitch = _pitch;
    }

    /// <summary>
    /// Immediately set the focus point and optionally the distance (if distance &gt; 0).
    /// Use this to focus the camera on a selected object.
    /// </summary>
    public void SetFocus(Vector3 focus, double? distance = null, double? minDistanceOverride = null)
    {
        _focusProvider = null;
        // Clear any follow-smoothing timer when manually setting focus
        _followSmoothingRemaining = 0f;
        _followSmoothingTotal = 0f;
        _targetFocus = focus;
        // Store optional per-focus min distance override (used to prevent clipping into focused objects)
        _focusMinDistanceOverride = minDistanceOverride;
        if (distance > 0f)
        {
            // When an override exists, ensure initial target distance respects it
            double effectiveMin = _focusMinDistanceOverride ?? MinDistance;
            _targetDistance = Math.Clamp(distance.Value, effectiveMin, MaxDistance);
            // Ensure the camera's far plane can see the desired focus distance.
            try { _camera.EnsureFar(_targetDistance * 2.0); } catch { }
        }
    }

    /// <summary>
    /// Set a live provider that supplies the focus point each frame. Useful to follow a moving entity.
    /// Passing null as provider will clear the live follow and use the static focus instead.
    /// </summary>
    // providerKey: optional object used to identify the target being followed. If the
    // same key is passed again, the call will not restart follow smoothing (prevents
    // re-selecting the same object and retriggering animations). If providerKey is
    // null, we fall back to comparing the delegate reference.
    public void SetFocusProvider(Func<Vector3Double>? provider, double? distance = null, double? minDistanceOverride = null, object? providerKey = null)
    {
        // If the caller requests clearing the provider, clear everything.
        if (provider == null)
        {
            _focusProvider = null;
            _currentProviderKey = null;
            _followSmoothingRemaining = 0f;
        }
        else
        {
            // If a providerKey is supplied and matches the current one, treat this as
            // a redundant selection. Allow distance updates but don't reset smoothing.
            bool isSameTarget = false;
            if (providerKey != null && _currentProviderKey != null)
            {
                isSameTarget = providerKey.Equals(_currentProviderKey);
            }
            else if (providerKey == null && _focusProvider != null)
            {
                // No key supplied; fall back to comparing delegate references.
                isSameTarget = ReferenceEquals(_focusProvider, provider);
            }

            if (isSameTarget)
            {
                // Update distance/minDistance override if requested, but do not restart smoothing.
                _focusProvider = provider;
                _currentProviderKey = providerKey ?? _currentProviderKey;
                if (distance > 0f)
                {
                    double effectiveMin = _focusMinDistanceOverride ?? MinDistance;
                    _targetDistance = Math.Clamp(distance.Value, effectiveMin, MaxDistance);
                }
                if (minDistanceOverride.HasValue)
                {
                    _focusMinDistanceOverride = minDistanceOverride;
                }
                return;
            }

            _focusProvider = provider;
            _currentProviderKey = providerKey;
        }
        // Store optional per-focus min distance override for live-follow
        _focusMinDistanceOverride = minDistanceOverride;
        if (_focusProvider != null)
        {
            try
            {
                _targetFocus = _focusProvider();
            }
            catch { }

            // Determine whether we are switching providers. Use the provider key when
            // available; otherwise fallback to delegate reference equality.
            bool switched = false;
            if (providerKey != null)
            {
                // If previous key existed and is different, it's a switch. Otherwise
                // if there was no previous key, treat as a switch as well.
                switched = !object.Equals(_currentProviderKey, providerKey);
            }
            else
            {
                // No key: we already set _focusProvider above, compare delegate references
                // against the previously stored delegate (we compared earlier for redundancy)
                switched = true; // conservative: assume switch if we reached here
            }

            _followSmoothingRemaining = switched ? FollowSwitchSmoothingSeconds : InitialFollowSmoothingSeconds;
            _followSmoothingTotal = _followSmoothingRemaining;
        }

        if (distance > 0f)
        {
            double effectiveMin = _focusMinDistanceOverride ?? MinDistance;
            _targetDistance = Math.Clamp(distance.Value, effectiveMin, MaxDistance);
        }
    }

    /// <summary>
    /// Duration in seconds to smooth the initial transition when a live focus provider
    /// is set. Default is a short time so focusing a new object doesn't feel like a teleport.
    /// </summary>
    public float InitialFollowSmoothingSeconds { get; set; } = 0.10f;
    /// <summary>
    /// Smoothing rate used while actively tracking a live focus provider after the
    /// initial follow smoothing window. A high value (e.g., 60) makes tracking feel
    /// immediate while still applying per-frame exponential smoothing to avoid
    /// single-frame visual snaps.
    /// </summary>
    public float FollowTrackingSmoothingRate { get; set; } = 60.0f;
    /// <summary>
    /// When switching from one live provider to another, use this longer smoothing
    /// duration so the camera eases between tracked objects rather than snapping.
    /// </summary>
    public float FollowSwitchSmoothingSeconds { get; set; } = 1f;
    /// <summary>
    /// Maximum focus movement speed (world units per second) allowed when tracking a live target.
    /// This prevents single-frame large jumps when the provider's target moves suddenly.
    /// </summary>
    public float FollowMaxFocusSpeed { get; set; } = 500.0f;

    /// <summary>
    /// Maximum change in distance (world units per second) allowed when tracking a live target.
    /// </summary>
    public float FollowMaxDistanceDeltaPerSecond { get; set; } = 500.0f;

    /// <summary>
    /// Applies an origin shift (rebase offset) to the controller so its internal focus
    /// and distances remain consistent after the world has been rebased.
    /// The offset should be the same value subtracted from the camera and world positions.
    /// </summary>
    public void ApplyOriginShift(Vector3Double offset)
    {
        _targetFocus -= offset;
        _currentFocus -= offset;
    }

    /// <summary>
    /// Compute the distance-based pan scale. Uses _currentDistance/10 as the base
    /// and raises it to <see cref="PanDistanceScaleExponent"/>. Clamped to a small
    /// minimum to avoid zero/near-zero scaling.
    /// </summary>
    private double GetDistanceScale()
    {
        // base scale: distance divided by 10 (preserves existing behavior as baseline)
        double baseScale = Math.Max(0.01, _currentDistance / 10.0);
        // if exponent is ~1, returns baseScale; <1 reduces growth when zoomed out
        if (Math.Abs(PanDistanceScaleExponent - 1.0) < 1e-6)
        {
            return baseScale;
        }
        // Negative or zero exponent is not meaningful for scaling; treat <= 0 as 1
        if (PanDistanceScaleExponent <= 0.0)
        {
            return baseScale;
        }
        return Math.Pow(baseScale, PanDistanceScaleExponent);
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
        var kb = inputs.Keyboard;
        var mouse = inputs.Mouse;

        // On Windows, optionally clip the cursor to the window while performing camera drag/edge-pan
        // to prevent the cursor from leaving the fullscreen/primary display when the user has multiple monitors.
        // Use SDL relative mouse mode (cross-platform) to confine the cursor and get relative motion
        // while dragging or edge-panning. Toggle via the Window API exposed by MoonWorks.
        bool wantRelative = false;

        // Edge-triggered relative mode is only enabled when the window is fullscreen.
        if (EdgePanEnabled && window.ScreenMode == ScreenMode.Fullscreen && !mouse.RightButton.IsDown && !mouse.MiddleButton.IsDown)
        {
            int width = (int)window.Width;
            int height = (int)window.Height;
            int x = mouse.X;
            int y = mouse.Y;
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                float threshold = MathF.Max(1, EdgePanThreshold);
                if (x <= threshold || x >= width - threshold || y <= threshold || y >= height - threshold)
                {
                    wantRelative = true;
                }
            }
        }

        if (mouse.MiddleButton.IsDown || mouse.RightButton.IsDown)
        {
            wantRelative = true;
        }

        try
        {
            if (wantRelative && !_isRelativeMouseMode)
            {
                window.SetRelativeMouseMode(true);
                _isRelativeMouseMode = true;
            }
            else if (!wantRelative && _isRelativeMouseMode)
            {
                window.SetRelativeMouseMode(false);
                _isRelativeMouseMode = false;
            }
        }
        catch { }

        // NOTE: we no longer sample the provider here directly. Sampling is
        // performed later (after dt is known) and blended into _targetFocus to
        // avoid a single-frame jump when a moving object is focused.

        // Speed modifier
        float speedMult = kb.IsHeld(KeyCode.LeftShift) ? 4.0f : 1.0f;

        // --- Rotation (orbit) ---
        if (mouse.RightButton.IsDown)
        {
            var md = new Vector2Double(mouse.DeltaX, mouse.DeltaY);
            if (md != Vector2Double.Zero)
            {
                // Write to target angles so we can smoothly interpolate the current angles
                _targetYaw += md.X * RotateSensitivity;
                _targetPitch += -md.Y * RotateSensitivity; // invert Y for natural feel
                                                           // Clamp target pitch slightly inside poles to avoid singularity
                _targetPitch = Math.Clamp(_targetPitch, (-MathF.PI / 2f) + 0.01f, (MathF.PI / 2f) - 0.01f);
            }
        }

        // --- Panning ---
        // Mouse middle drag pans the focus in the camera plane. We scale pan by distance so
        // that panning feels consistent at different zoom levels.
        if (mouse.MiddleButton.IsDown)
        {
            var md = new Vector2Double(mouse.DeltaX, mouse.DeltaY);
            if (md != Vector2Double.Zero)
            {
                // move opposite to drag (dragging right moves world left)
                var right = _camera.Right;
                var camUp = _camera.Up;
                double distanceScale = GetDistanceScale();
                var pan = ((-right * md.X) + (camUp * md.Y)) * (PanSpeed * distanceScale) * speedMult;
                _targetFocus += pan;

                // vertical middle-drag also adjusts zoom a bit for convenience
                _targetDistance += -md.Y * (ZoomSpeed * 0.01f) * speedMult;

                // Manual middle-drag indicates an intentional user pan; detach any live follow
                // and clear the follow smoothing timer so the manual pan uses the regular
                // smoothing behaviour (avoids teleporting but still feels responsive).
                if (_focusProvider != null)
                {
                    _focusProvider = null;
                    _focusMinDistanceOverride = null;
                    _followSmoothingRemaining = 0f;
                    _followSmoothingTotal = 0f;
                }
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

        // If the user manually pans using keyboard, break any live-follow provider
        // and snap current focus/distance so the camera detaches immediately.
        if (_focusProvider != null && (forward != 0f || rightDir != 0f || up != 0f))
        {
            // Detach live-follow when the user starts keyboard panning. Do not snap
            // immediately; allow the existing smoothing to interpolate the camera so
            // movement feels smooth rather than instant.
            _focusProvider = null;
            _focusMinDistanceOverride = null;
            _followSmoothingRemaining = 0f;
            _followSmoothingTotal = 0f;
        }

        if (forward != 0f || rightDir != 0f || up != 0f)
        {
            // Project forward onto camera's XZ plane for more intuitive pan (prevent flying when pitched)
            var camForward = _camera.Forward;
            var camForwardXZ = Vector3Double.Normalize(new Vector3Double(camForward.X, 0f, camForward.Z));
            var panVec = (camForwardXZ * forward) + (_camera.Right * rightDir) + (Vector3Double.UnitY * up);
            double distanceScale = GetDistanceScale();
            _targetFocus += panVec * (PanSpeed * distanceScale) * speedMult;
        }

        // --- Edge Panning (RTS-style) ---
        // When the mouse nears the edges of the window, pan the camera in the camera plane.
        // Disabled while holding middle or right mouse buttons to avoid conflicting with drag controls.
        if (EdgePanEnabled && window.ScreenMode == ScreenMode.Fullscreen && !mouse.RightButton.IsDown && !mouse.MiddleButton.IsDown)
        {
            int width = (int)window.Width;
            int height = (int)window.Height;
            int x = mouse.X;
            int y = mouse.Y;
            // Only if the pointer is inside the window bounds
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                double threshold = Math.Max(1, EdgePanThreshold);
                double leftStrength = x <= threshold ? (threshold - x) / threshold : 0f;
                double rightStrength = x >= width - threshold ? (x - (width - threshold)) / threshold : 0f;
                double topStrength = y <= threshold ? (threshold - y) / threshold : 0f;
                double bottomStrength = y >= height - threshold ? (y - (height - threshold)) / threshold : 0f;

                if (leftStrength > 0f || rightStrength > 0f || topStrength > 0f || bottomStrength > 0f)
                {
                    // Pan in camera plane. Forward is camera forward projected onto XZ.
                    var camForward = _camera.Forward;
                    var camForwardXZ = new Vector3Double(camForward.X, 0f, camForward.Z);
                    if (camForwardXZ.LengthSquared() > 1e-6f)
                    {
                        camForwardXZ = Vector3Double.Normalize(camForwardXZ);
                    }

                    var right = _camera.Right;
                    double distanceScale = GetDistanceScale();
                    double speed = EdgePanSpeed > 0f ? EdgePanSpeed : PanSpeed;

                    // Left/right pan
                    var pan = Vector3Double.Zero;
                    pan += -right * leftStrength;
                    pan += right * rightStrength;
                    // Top/bottom: top moves forward, bottom moves backward
                    pan += camForwardXZ * topStrength;
                    pan += -camForwardXZ * bottomStrength;

                    if (pan != Vector3Double.Zero)
                    {
                        _targetFocus += pan * (speed * distanceScale) * speedMult;
                    }
                }
            }
        }

        // --- Zoom ---
        // Compute a proximity-based scale so zoom steps become smaller (more granular)
        // as the camera approaches the minimum distance (i.e., when zoomed in).
        // normalized in [0,1]: 0 == at MinDistance, 1 == at MaxDistance
        double range = Math.Max(1e-5f, MaxDistance - MinDistance);
        double proximityUnclamped = (_currentDistance - MinDistance) / range;
        double proximity = Math.Max(0.0, Math.Min(1.0, proximityUnclamped));
        // scale in (0.1 .. 1.0] so close distances produce smaller steps.
        // Use a sqrt curve so zoom-out accelerates faster while zoom-in remains granular.
        double proximityCurve = Math.Sqrt(proximity);
        double proximityScale = 0.1f + (0.9f * proximityCurve);

        // Keyboard zoom (Z/X) — scale step by proximityScale
        if (kb.IsDown(KeyCode.Z)) _targetDistance -= ZoomSpeed * speedMult * proximityScale;
        if (kb.IsDown(KeyCode.X)) _targetDistance += ZoomSpeed * speedMult * proximityScale;

        // --- Scroll-wheel zoom ---
        // MoonWorks provides `mouse.Wheel` as an integer delta for this frame.
        if (mouse.Wheel != 0)
        {
            // Negative wheel typically means zoom in on many platforms, so invert to match other zoom controls
            var rawDelta = -mouse.Wheel * ZoomSpeed * 0.1f * speedMult * ScrollZoomMultiplier * proximityScale;
            // Clamp individual scroll step to avoid huge jumps on high-dpi / sensitive mice
            var clamped = Math.Max(-MaxScrollZoomStep, Math.Min(MaxScrollZoomStep, rawDelta));
            _targetDistance += clamped;
        }

        // Clamp target distance, respecting any per-focus minimum override to avoid clipping into focused objects
        double effectiveMinDistance = _focusMinDistanceOverride ?? MinDistance;
        _targetDistance = Math.Clamp(_targetDistance, effectiveMinDistance, MaxDistance);

        // --- Smooth interpolation for focus/distance ---
        // Use frame delta so smoothing behaves consistently across varying frame rates.
        // Exponential smoothing: t = 1 - exp(-rate * dt)
        double dt = delta.TotalSeconds;
        double t;
        if (dt <= 0f)
        {
            // No time passed: snap to target to avoid stalling.
            t = 1f;
        }
        else if (Smoothing <= 0f)
        {
            // Zero/negative smoothing treated as instant (no smoothing)
            t = 1f;
        }
        else
        {
            t = 1f - Math.Exp(-Smoothing * dt);
        }

        if (_focusProvider != null)
        {
            // Sample provider once and blend into _targetFocus to avoid a last-frame
            // jump when the provider's position changes between frames. We use a
            // small smoothing factor derived from FollowTrackingSmoothingRate so the
            // target follows responsively but without jitter.
            Vector3Double providerPos;
            try
            {
                providerPos = _focusProvider();
            }
            catch
            {
                providerPos = _targetFocus; // fallback: keep existing target
            }

            // If we're still in the initial follow-smoothing window, smoothly
            // interpolate the camera from its current position to the provider.
            // This prevents an abrupt teleport when switching targets.
            if (_followSmoothingRemaining > 0f)
            {
                // Compute activeRate that eases from Smoothing -> FollowTrackingSmoothingRate
                double activeRate;
                if (_followSmoothingTotal > 0f)
                {
                    double r = _followSmoothingRemaining / _followSmoothingTotal;
                    if (r < 0f) r = 0f;
                    if (r > 1f) r = 1f;
                    activeRate = (FollowTrackingSmoothingRate * (1f - r)) + (Smoothing * r);
                }
                else
                {
                    activeRate = FollowTrackingSmoothingRate;
                }

                // Exponential step
                double localT = 1f;
                if (dt > 0f && activeRate > 0f)
                {
                    localT = 1f - Math.Exp(-activeRate * dt);
                }

                _targetFocus = providerPos;

                var desiredFocus = Vector3Double.Lerp(_currentFocus, _targetFocus, localT);
                var desiredDistance = _currentDistance + ((_targetDistance - _currentDistance) * localT);

                // Apply per-frame clamps to avoid huge instantaneous jumps
                if (dt > 0f && FollowMaxFocusSpeed > 0f)
                {
                    var maxMove = FollowMaxFocusSpeed * dt;
                    var focusDelta = desiredFocus - _currentFocus;
                    var dist = focusDelta.Length();
                    if (dist > maxMove)
                    {
                        desiredFocus = _currentFocus + (Vector3Double.Normalize(focusDelta) * maxMove);
                    }
                }

                if (dt > 0f && FollowMaxDistanceDeltaPerSecond > 0f)
                {
                    var maxDistChange = FollowMaxDistanceDeltaPerSecond * dt;
                    var distDelta = desiredDistance - _currentDistance;
                    if (Math.Abs(distDelta) > maxDistChange)
                    {
                        desiredDistance = _currentDistance + (Math.Sign(distDelta) * maxDistChange);
                    }
                }

                _currentFocus = desiredFocus;
                _currentDistance = desiredDistance;

                _followSmoothingRemaining = Math.Max(0f, _followSmoothingRemaining - dt);
                if (_followSmoothingRemaining == 0f)
                {
                    _followSmoothingTotal = 0f;
                }
            }
            else
            {
                // Finished initial smoothing: snap to provider each frame so the
                // camera tracks the object immediately (no temporal smoothing).
                _targetFocus = providerPos;
                var desiredFocus = providerPos;
                var desiredDistance = _targetDistance;

                // Still apply per-frame clamps to avoid teleporting if the provider
                // teleports or moves extremely fast.
                if (dt > 0f && FollowMaxFocusSpeed > 0f)
                {
                    var maxMove = FollowMaxFocusSpeed * dt;
                    var focusDelta = desiredFocus - _currentFocus;
                    var dist = focusDelta.Length();
                    if (dist > maxMove)
                    {
                        desiredFocus = _currentFocus + (Vector3Double.Normalize(focusDelta) * maxMove);
                    }
                }

                if (dt > 0f && FollowMaxDistanceDeltaPerSecond > 0f)
                {
                    var maxDistChange = FollowMaxDistanceDeltaPerSecond * dt;
                    var distDelta = desiredDistance - _currentDistance;
                    if (Math.Abs(distDelta) > maxDistChange)
                    {
                        desiredDistance = _currentDistance + (Math.Sign(distDelta) * maxDistChange);
                    }
                }

                _currentFocus = desiredFocus;
                _currentDistance = desiredDistance;
            }
        }
        else
        {
            _currentFocus = Vector3Double.Lerp(_currentFocus, _targetFocus, t);
            _currentDistance += ((_targetDistance - _currentDistance) * t);
        }

        // --- Smooth interpolation for rotation (yaw/pitch) ---
        if (SmoothRotation && RotationSmoothing > 0f)
        {
            double tRot;
            if (dt <= 0f)
            {
                tRot = 1f;
            }
            else
            {
                tRot = 1f - Math.Exp(-RotationSmoothing * dt);
            }

            // Manual Lerp: current + (target - current) * t
            _yaw += ((_targetYaw - _yaw) * tRot);
            _pitch += ((_targetPitch - _pitch) * tRot);
        }
        else
        {
            // No smoothing: snap current angles to targets
            _yaw = _targetYaw;
            _pitch = _targetPitch;
        }

        // Apply yaw/pitch to camera and set position relative to focus
        _camera.Yaw = _yaw;
        _camera.Pitch = _pitch;

        // Put camera at focus - forward * distance
        _camera.Position = _currentFocus - (_camera.Forward * _currentDistance);
    }

    /// <summary>
    /// Immediately snap the current rotation to the target rotation (no smoothing).
    /// Useful to reset after tweaking smoothing settings.
    /// </summary>
    public void SnapRotationToTarget()
    {
        _yaw = _targetYaw;
        _pitch = _targetPitch;
    }

    /// <summary>
    /// Set the current and target rotation to match the camera's present yaw/pitch.
    /// Useful to initialize targets after externally changing the camera.
    /// </summary>
    public void SnapRotationToCamera()
    {
        _yaw = _camera.Yaw;
        _pitch = _camera.Pitch;
        _targetYaw = _yaw;
        _targetPitch = _pitch;
    }
}

