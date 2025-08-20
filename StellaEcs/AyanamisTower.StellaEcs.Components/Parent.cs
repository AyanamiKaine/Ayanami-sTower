using System;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Represents the parent component of an entity.
/// </summary>
public class Parent
{
    /// <summary>
    /// Gets or sets the entity that is the parent of this entity.
    /// </summary>
    public Entity Entity { get; set; }
}
