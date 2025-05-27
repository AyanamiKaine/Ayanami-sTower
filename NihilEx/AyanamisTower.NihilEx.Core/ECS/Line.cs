using System;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents a line segment defined by two points.
/// </summary>
/// <param name="Start">The starting point of the line.</param>
/// <param name="Stop">The ending point of the line.</param>
public record struct Line(Point Start, Point Stop);
