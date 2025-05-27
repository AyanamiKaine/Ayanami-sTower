using System;
using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents a point in 2D space using a Vector2.
/// </summary>
/// <param name="Value">The underlying Vector2 value.</param>
public record struct Point(Vector2 Value);
