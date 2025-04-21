// Holds the calculated 2D projected vertices for rendering
using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Holds the calculated 2D projected vertices for rendering.
/// </summary>
/// <param name="ProjectedVertices">The array of 2D vectors representing the projected vertices.</param>
public record struct ProjectedMesh(Vector2[] ProjectedVertices);
