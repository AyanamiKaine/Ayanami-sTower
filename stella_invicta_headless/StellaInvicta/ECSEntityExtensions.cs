using Flecs.NET.Core;
using NLog;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta;

/// <summary>
/// Provides extension methods for Entity operations in the ECS (Entity Component System) framework.
/// </summary>
/// <remarks>
/// This static class contains utility methods that extend the functionality of Entity objects,
/// particularly for checking connections and relationships between entities.
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
}