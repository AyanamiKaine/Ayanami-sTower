using System.Numerics;
namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents a position in 3D space.
/// </summary>
/// <param name="Value">The vector representing the position.</param>
public record struct Position3D(Vector3 Value);
