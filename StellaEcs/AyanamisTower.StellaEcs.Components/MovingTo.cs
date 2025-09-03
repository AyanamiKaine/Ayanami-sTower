using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Marks an entity as moving toward a target point at a given speed with an arrival radius.
/// Systems and UI can inspect this to visualize or adjust move commands.
/// </summary>
public struct MovingTo
{
    /// <summary>World-space destination.</summary>
    public Vector3Double Target;
    /// <summary>Desired linear speed (units/sec).</summary>
    public double Speed;
    /// <summary>Distance at which we consider the entity arrived and should stop.</summary>
    public double ArriveRadius;

    /// <summary>
    /// Creates a new MovingTo directive.
    /// </summary>
    /// <param name="target">World-space destination.</param>
    /// <param name="speed">Desired linear speed (units/sec).</param>
    /// <param name="arriveRadius">Distance at which we consider arrival and stop.</param>
    public MovingTo(Vector3Double target, double speed, double arriveRadius = 0.1)
    {
        Target = target;
        Speed = speed;
        ArriveRadius = arriveRadius;
    }
}
