using System;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Represents the parent component of an entity.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Parent"/> class.
/// </remarks>
/// <param name="entity"></param>
public struct Parent(Entity entity)
{
    /// <summary>
    /// Gets or sets the entity that is the parent of this entity.
    /// </summary>
    public Entity Entity { get; set; } = entity;
}
