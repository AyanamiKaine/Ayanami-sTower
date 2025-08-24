using System;
using AyanamisTower.StellaEcs.StellaInvicta;
using BepuPhysics.Collidables;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

/// <summary>
/// defines a collision shape for an entity.
/// </summary>
/// <param name="shape"></param>
public readonly struct CollisionShape(IShape shape)
{
    /// <summary>
    /// Gets the shape associated with the collision shape.
    /// </summary>
    public IShape Shape { get; } = shape;
}
