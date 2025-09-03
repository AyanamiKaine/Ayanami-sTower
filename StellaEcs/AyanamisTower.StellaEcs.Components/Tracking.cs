using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Marks an entity as tracking (following) another entity.
/// The system will read the target entity's Position3D each update and
/// steer the tracking entity toward the target's current position.
/// </summary>
public struct Tracking
{
    /// <summary>Entity being tracked (target).</summary>
    public Entity Target;

    /// <summary>Desired linear speed (units/sec).</summary>
    public double Speed;

    /// <summary>Distance at which we consider the tracker has reached the target and should stop.</summary>
    public double ArriveRadius;
    /// <summary>
    /// Initializes a new instance of the <see cref="Tracking"/> struct.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="speed"></param>
    /// <param name="arriveRadius"></param>
    public Tracking(Entity target, double speed, double arriveRadius = 0.1)
    {
        Target = target;
        Speed = speed;
        ArriveRadius = arriveRadius;
    }
}
