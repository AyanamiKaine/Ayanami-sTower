using Flecs.NET.Core;
using NLog;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta;

/// <summary>
/// Extension methods for Entity class providing helper methods for connection, orbiting, mining, and component management.
/// </summary>
/// <remarks>
/// This class contains various utility methods to simplify common operations with entities in the ECS architecture.
/// It handles entity relationships, resource mining, and component access patterns.
/// </remarks>
public static class ECSEntityExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Determines whether two entities are connected to each other.
    /// </summary>
    /// <param name="entityA">The first entity to check.</param>
    /// <param name="entityB">The second entity to check.</param>
    /// <returns>True if entityA is connected to entityB, false otherwise.</returns>
    /// <remarks>
    /// This extension method checks if entityA has a connection component referencing entityB.
    /// </remarks>
    public static bool ConnectedTo(this Entity entityA, Entity entityB)
    {
        return entityA.Has<ConnectedTo>(entityB);
    }

    /// <summary>
    /// Determines whether one entity orbits another entity.
    /// </summary>
    /// <param name="entityA">The entity that may be orbiting.</param>
    /// <param name="entityB">The entity that may be orbited.</param>
    /// <returns>True if entityA orbits entityB, false otherwise.</returns>
    public static bool Orbits(this Entity entityA, Entity entityB)
    {
        return entityA.Has<Orbits>(entityB);
    }

    /// <summary>
    /// Mines a resource of type T from the target entity.
    /// </summary>
    /// <typeparam name="T">The type of resource to mine</typeparam>
    /// <param name="miner">The entity performing the mining action</param>
    /// <param name="entityWithResources">The target entity containing the resources to be mined</param>
    /// <returns>The miner entity</returns>
    public static Entity Mine<T>(this Entity miner, Entity entityWithResources) where T : IResource
    {
        if (entityWithResources.Has<T>())
        {
            ref T resource = ref entityWithResources.GetMut<T>();  // Get mutable reference
            resource.Subtract(20);
            return miner;
        }
        else
        {
            Logger.ConditionalDebug($"Miner: {miner.Name()} tried to mine resource but entity with resource({entityWithResources.Name()}) does not have the resource!");
            return miner;
        }
    }

    /// <summary>
    /// Gets the component from an entity, should the entity not have the component it will try setting it.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static T GetOrSet<T>(this Entity e) where T : new()
    {
        if (e.Has<T>())
        {
            return e.Get<T>();
        }
        else
        {
            e.Set<T>(new());
            return e.Get<T>();
        }
    }

    /// <summary>
    /// Gets mutable managed reference for the component from an entity, should the entity not have the component it will try first setting it.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static ref T GetMutOrSet<T>(this Entity e) where T : new()
    {
        if (e.Has<T>())
        {
            return ref e.GetMut<T>();
        }
        else
        {
            e.Set<T>(new());
            return ref e.GetMut<T>();
        }
    }
}