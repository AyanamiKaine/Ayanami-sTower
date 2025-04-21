using System;
using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents the size of an entity using a Vector2.
/// </summary>
/// <param name="Value">The size value.</param>
public record struct Size2D(Vector2 Value);
