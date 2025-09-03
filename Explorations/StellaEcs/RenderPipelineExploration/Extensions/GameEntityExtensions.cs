using System;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace StellaInvicta.Extensions;

/// <summary>
/// Extension methods for game entities. Like move. We put all methods we would like on entities
/// here.
/// </summary>
public static class GameEntityExtensions
{
    /// <summary>
    /// Issues a simple move command: points Velocity3D toward the target at the given speed and records intent in a MovingTo component.
    /// - If the entity has Position3D and Velocity3D, Velocity3D is updated to head toward target.
    /// - A <see cref="MovingTo"/> component is set to keep the target and arrival radius.
    /// - If already within arriveRadius, Velocity3D is zeroed and MovingTo is removed.
    /// Returns the entity for chaining.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    /// <param name="target">World-space target point.</param>
    /// <param name="speed">Desired linear speed (units/sec).</param>
    /// <param name="arriveRadius">Distance within which we consider arrival and stop movement.</param>
    public static Entity MoveTo(this Entity entity, Vector3Double target, double speed, double arriveRadius = 0.1)
    {
        // Always store the command intent so systems/UI can reflect it.
        entity.Set(new MovingTo(target, speed, arriveRadius));

        // Only compute velocity when both components exist, per the user's rule.
        if (entity.Has<Position3D>() && entity.Has<Velocity3D>())
        {
            var pos = entity.GetCopy<Position3D>().Value;
            var toTarget = target - pos;
            var dist = toTarget.Length();

            if (dist <= arriveRadius)
            {
                // Arrived: stop and clear intent.
                entity.Set(new Velocity3D(0, 0, 0));
                entity.Remove<MovingTo>();
                return entity;
            }

            // Set velocity toward target at desired speed.
            var dir = Vector3Double.Normalize(toTarget);
            var vel = dir * speed;
            entity.Set(new Velocity3D(vel));
        }

        return entity;
    }

    /// <summary>
    /// Overload taking a Position3D target.
    /// </summary>
    public static Entity MoveTo(this Entity entity, Position3D target, double speed, double arriveRadius = 0.1)
        => MoveTo(entity, target.Value, speed, arriveRadius);

    /// <summary>
    /// Cancels any active MoveTo intent and zeros Velocity3D if present.
    /// </summary>
    public static Entity StopMovement(this Entity entity)
    {
        if (entity.Has<Velocity3D>())
        {
            entity.Set(new Velocity3D(0, 0, 0));
        }
        if (entity.Has<MovingTo>())
        {
            entity.Remove<MovingTo>();
        }
        return entity;
    }

    /// <summary>
    /// Begins tracking another entity (follows it). The tracked entity must have Position3D.
    /// Stores intent in a <see cref="Tracking"/> component and updates Velocity3D if present.
    /// If the target entity is already within arriveRadius, tracking is not added and movement is stopped.
    /// </summary>
    public static Entity TrackTo(this Entity entity, Entity target, double speed, double arriveRadius = 0.1)
    {
        // Store intent
        entity.Set(new Tracking(target, speed, arriveRadius));

        // If we have position & velocity, set immediate velocity toward current target position
        if (entity.Has<Position3D>() && entity.Has<Velocity3D>())
        {
            if (!target.Has<Position3D>())
            {
                // Can't compute velocity without target position
                return entity;
            }

            var pos = entity.GetCopy<Position3D>().Value;
            var targetPos = target.GetCopy<Position3D>().Value;
            var toTarget = targetPos - pos;
            var dist = toTarget.Length();

            /*   
            I dont think tracking should automatic stop when the target is within arriveRadius
            it instead should stay in the arrive radius stationed around the object it currently tracks.
            if (dist <= arriveRadius)
            {
                entity.Set(new Velocity3D(0, 0, 0));
                entity.Remove<Tracking>();
                return entity;
            }
            
            */


            var dir = Vector3Double.Normalize(toTarget);
            entity.Set(new Velocity3D(dir * speed));
        }

        return entity;
    }

    /// <summary>
    /// Cancels any active TrackTo intent and zeros Velocity3D if present.
    /// </summary>
    public static Entity StopTracking(this Entity entity)
    {
        if (entity.Has<Velocity3D>())
        {
            entity.Set(new Velocity3D(0, 0, 0));
        }
        if (entity.Has<Tracking>())
        {
            entity.Remove<Tracking>();
        }
        return entity;
    }

}
