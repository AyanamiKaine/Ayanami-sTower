using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.StellaInvicta;

/// <summary>
/// Spatial query helpers for <see cref="World"/> based on Position3D components.
/// </summary>
public static class WorldSpatialExtensions
{
    /// <summary>
    /// Returns all entities that have a <see cref="Position3D"/> and lie within the given radius of the origin entity's Position3D.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="origin">The origin entity. Must have a Position3D component.</param>
    /// <param name="radius">Inclusive radius in world units.</param>
    /// <param name="includeOrigin">If true, the origin entity is included in the results when in range.</param>
    /// <returns>A list of entities within <paramref name="radius"/> of <paramref name="origin"/>.</returns>
    public static List<Entity> EntitiesWithinRange(this World world, Entity origin, double radius, bool includeOrigin = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsEntityValid(origin)) return [];
        if (radius <= 0) return [];

        // If origin has no Position3D, nothing can be computed.
        if (!origin.Has<Position3D>()) return [];
        var originPos = origin.GetCopy<Position3D>().Value;
        double r2 = radius * radius;

        var results = new List<Entity>();
        foreach (var e in world.Query(typeof(Position3D)))
        {
            if (!includeOrigin && e.Equals(origin))
                continue;

            // Fast path: retrieve positions and compare squared distance.
            var pos = e.GetCopy<Position3D>().Value;
            if (Vector3Double.DistanceSquared(pos, originPos) <= r2)
            {
                results.Add(e);
            }
        }
        return results;
    }

    /// <summary>
    /// Returns all entities that have a <see cref="Position3D"/> and lie within the given radius of a world-space center.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="center">World-space center.</param>
    /// <param name="radius">Inclusive radius in world units.</param>
    /// <returns>A list of entities within <paramref name="radius"/> of <paramref name="center"/>.</returns>
    public static List<Entity> EntitiesWithinRange(this World world, Vector3Double center, double radius)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (radius <= 0) return [];

        double r2 = radius * radius;
        var results = new List<Entity>();
        foreach (var e in world.Query(typeof(Position3D)))
        {
            var pos = e.GetCopy<Position3D>().Value;
            if (Vector3Double.DistanceSquared(pos, center) <= r2)
            {
                results.Add(e);
            }
        }
        return results;
    }
}
