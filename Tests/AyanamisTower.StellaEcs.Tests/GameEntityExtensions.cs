using System;
using AyanamisTower.StellaEcs.Attributes;
using AyanamisTower.StellaEcs.Extensions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Examples;

/// <summary>
/// Basic game entity extensions demonstrating the ergonomic API for entity operations.
/// These methods showcase the declarative component system with clean, readable code.
/// </summary>
[EntityExtensions]
public static class GameEntityExtensions
{
    /// <summary>
    /// Moves an entity by applying its velocity to its position.
    /// Requires: Position (mutable), Velocity (readonly)
    /// </summary>
    [EntityExtension]
    [With<Position>(mutable: true)]
    [With<Velocity>]
    public static void Move(this Entity entity, float deltaTime)
    {
        entity.WithComponentsMixed<Position, Velocity>((ref Position pos, in Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
            pos.Z += vel.Z * deltaTime;
        });
    }

    /// <summary>
    /// Applies damage to an entity's health.
    /// Requires: Health (mutable), Damage (readonly)
    /// </summary>
    [EntityExtension]
    [With<Health>(mutable: true)]
    [With<Damage>]
    public static void ApplyDamage(this Entity entity)
    {
        entity.WithComponentsMixed<Health, Damage>((ref Health health, in Damage damage) =>
        {
            health.Current = Math.Max(0, health.Current - damage.Amount);
        });
    }

    /// <summary>
    /// Checks if an entity is at a specific position.
    /// Requires: Position (readonly)
    /// </summary>
    [EntityExtension]
    [With<Position>]
    public static bool IsAtPosition(this Entity entity, float x, float y, float z)
    {
        return entity.With<Position, bool>(pos =>
            Math.Abs(pos.X - x) < 0.001f &&
            Math.Abs(pos.Y - y) < 0.001f &&
            Math.Abs(pos.Z - z) < 0.001f);
    }

    /// <summary>
    /// Gets the distance to another entity.
    /// Requires: Position (readonly) on both entities
    /// </summary>
    [EntityExtension]
    [With<Position>]
    public static float DistanceTo(this Entity entity, Entity other)
    {
        if (!other.Has<Position>())
            throw new InvalidOperationException("Target entity must have Position component");

        var pos1 = entity.Get<Position>();
        var pos2 = other.Get<Position>();

        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Heals an entity to full health.
    /// Requires: Health (mutable)
    /// </summary>
    [EntityExtension]
    [With<Health>(mutable: true)]
    public static void HealToFull(this Entity entity)
    {
        entity.WithComponentMutable<Health>((ref Health health) =>
        {
            health.Current = health.Max;
        });
    }

    /// <summary>
    /// Safely tries to move an entity, only if it has both Position and Velocity.
    /// Returns true if the move was applied.
    /// </summary>
    [EntityExtension(AllowPartial = true)]
    public static bool TryMove(this Entity entity, float deltaTime)
    {
        return entity.TryWith<Position, Velocity>((pos, vel) =>
        {
            entity.Set(new Position(
                pos.X + vel.X * deltaTime,
                pos.Y + vel.Y * deltaTime,
                pos.Z + vel.Z * deltaTime
            ));
        });
    }

    /// <summary>
    /// Complex operation that works with multiple components.
    /// Demonstrates how to chain operations ergonomically.
    /// </summary>
    [EntityExtension]
    public static void UpdateCombat(this Entity entity, float deltaTime)
    {
        // Move the entity - using declarative component requirements
        entity.Move(deltaTime);  // Will only execute if Position + Velocity present

        // Apply damage if present - using declarative component requirements  
        entity.ApplyDamage();    // Will only execute if Health + Damage present

        // Remove damage after applying it (single-use) - only if damage was applied
        entity.WithComponent<Damage>(_ => entity.Remove<Damage>());

        // Check if entity died - using component macro
        entity.WithComponent<Health>(health =>
        {
            if (health.Current <= 0)
            {
                Console.WriteLine($"Entity {entity} has died!");
                // Could trigger death events here
            }
        });
    }
}
