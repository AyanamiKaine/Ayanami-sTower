using System;
using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents a position in 2D space.
/// </summary>
/// <param name="Value">The vector representing the position.</param>
public record struct Position2D(Vector2 Value);
