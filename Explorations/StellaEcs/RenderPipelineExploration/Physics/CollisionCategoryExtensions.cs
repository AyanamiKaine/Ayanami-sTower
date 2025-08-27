using System;
using AyanamisTower.StellaEcs.StellaInvicta.Components;

namespace StellaInvicta.Physics;

/// <summary>
/// Extension methods for <see cref="CollisionCategory"/>.
/// </summary>
public static class CollisionCategoryExtensions
{
    /// <summary>
    /// Create a CollisionLayer from named categories (category and mask).
    /// Example: CollisionCategory.Sun.ToLayer(CollisionCategory.None) // Sun collides with nobody
    /// Example: CollisionCategory.Player.ToLayer(CollisionCategory.Enemy | CollisionCategory.Default)
    /// </summary>
    public static CollisionLayer ToLayer(this CollisionCategory category, CollisionCategory mask)
        => new CollisionLayer((uint)category, (uint)mask);

    /// <summary>
    /// Convenience: category collides with everything.
    /// </summary>
    public static CollisionLayer ToLayerAll(this CollisionCategory category)
        => new CollisionLayer((uint)category, (uint)CollisionCategory.All);
}

/* Example Usage

Sun that collides with nothing:
.Set(CollisionCategory.Sun.ToLayer(CollisionCategory.None))

Asteroid that collides with default objects:
.Set(CollisionCategory.Asteroid.ToLayer(CollisionCategory.Default))

Player collides with enemies and pickups:
.Set(CollisionCategory.Player.ToLayer(CollisionCategory.Enemy | CollisionCategory.Default))

*/