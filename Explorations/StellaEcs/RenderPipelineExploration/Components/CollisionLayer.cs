using System;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;

/// <summary>
/// Per-entity collision category/mask filter.
/// Category: the bit flag(s) this entity belongs to.
/// Mask: which categories this entity collides with.
/// </summary>
public struct CollisionLayer
{
    /// <summary>
    /// The category bitmask this entity belongs to.
    /// </summary>
    public uint Category;
    /// <summary>
    /// The mask of categories this entity collides with.
    /// </summary>
    public uint Mask;
    /// <summary>
    /// Creates a new collision layer with the specified category and mask.
    /// </summary>
    /// <param name="category">The category bitmask this entity belongs to.</param>
    /// <param name="mask">The mask of categories this entity collides with.</param>
    public CollisionLayer(uint category, uint mask)
    {
        Category = category;
        Mask = mask;
    }
}

