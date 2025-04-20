using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents a 3D rotation using a Vector3, typically interpreted as Euler angles.
/// </summary>
/// <param name="Value">The rotation value, often representing Euler angles (Pitch, Yaw, Roll).</param>
public record struct Rotation3D(Vector3 Value);
