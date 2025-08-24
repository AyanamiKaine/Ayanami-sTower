using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;
using BepuPhysics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// Manages the floating origin system to prevent floating point precision issues.
/// Periodically rebases all world coordinates by subtracting a large offset.
/// </summary>
public class FloatingOriginManager
{
    private Vector3Double _currentOrigin = Vector3Double.Zero;
    private readonly double _rebaseThreshold;
    private readonly World _world;
    private readonly Simulation _simulation;

    /// <summary>The current floating origin offset in world coordinates.</summary>
    public Vector3Double CurrentOrigin => _currentOrigin;
    /// <summary>True if a rebase operation is currently in progress.</summary>
    public bool IsRebasing { get; private set; }

    // Grid size used to snap rebase offsets. Snapping to integer values (1.0)
    // removes small fractional offsets that otherwise cause repeated tiny
    // float conversions and visible jitter when converting to single-precision.
    private readonly double _snapGridSize;

    /// <summary>
    /// Creates a new FloatingOriginManager.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <param name="simulation">The BepuPhysics simulation instance.</param>
    /// <param name="rebaseThreshold">Distance threshold that triggers a rebase.</param>
    /// <param name="snapGridSize">Grid size to snap rebase offsets to (default 1.0).
    /// Larger values cause coarser, less frequent fractional moves but keep numbers smaller.</param>
    public FloatingOriginManager(World world, Simulation simulation, double rebaseThreshold = 200.0, double snapGridSize = 1.0)
    {
        _world = world;
        _simulation = simulation;
        _rebaseThreshold = rebaseThreshold;
        _snapGridSize = Math.Max(1e-9, snapGridSize);
    }

    /// <summary>
    /// Checks if a rebase is needed based on the camera position and performs it if necessary.
    /// </summary>
    public bool Update(Vector3Double cameraPosition, out Vector3Double rebaseOffset)
    {
        var cameraDistance = new Vector3Double(cameraPosition.X, cameraPosition.Y, cameraPosition.Z).Length();

        if (cameraDistance > _rebaseThreshold)
        {
            rebaseOffset = SnapToGrid(cameraPosition);
            PerformRebase(rebaseOffset);
            return true;
        }

        rebaseOffset = default;
        return false;
    }

    /// <summary>
    /// Performs a floating origin rebase by shifting all entities and physics objects.
    /// </summary>
    private void PerformRebase(Vector3Double offset)
    {
        IsRebasing = true;

        _currentOrigin += offset;

        // Shift all Position3D values (which use double precision) by -offset.
        // This is the source of truth for the new positions.
        foreach (var entity in _world.Query(typeof(Position3D)))
        {
            var pos = entity.GetMut<Position3D>();
            var newPosD = pos.Value - offset;
            entity.Set(new Position3D(newPosD.X, newPosD.Y, newPosD.Z));
        }

        // Rebase physics objects to sync them with the new ECS positions.
        //RebasePhysicsObjects();

        IsRebasing = false;
    }

    /// <summary>
    /// Snaps a double-precision position to the configured grid size.
    /// </summary>
    private Vector3Double SnapToGrid(Vector3Double v)
    {
        if (_snapGridSize <= 0.0) return v;
        double inv = 1.0 / _snapGridSize;
        return new Vector3Double(
            Math.Round(v.X * inv) / inv,
            Math.Round(v.Y * inv) / inv,
            Math.Round(v.Z * inv) / inv);
    }

    /// <summary>
    /// Converts an absolute position to a relative position from the current origin.
    /// </summary>
    public Vector3 ToRelativePosition(Vector3Double absolutePosition)
    {
        return (Vector3)(absolutePosition - _currentOrigin);
    }

    /// <summary>
    /// Converts a relative position to an absolute position.
    /// </summary>
    public Vector3Double ToAbsolutePosition(Vector3 relativePosition)
    {
        return _currentOrigin + new Vector3Double(relativePosition.X, relativePosition.Y, relativePosition.Z);
    }
}
