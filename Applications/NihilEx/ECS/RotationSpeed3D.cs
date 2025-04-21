using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents a 3D rotation speed as a vector of angular velocities.
/// </summary>
/// <param name="SpeedRadPerSec">The rotation speed vector in radians per second.</param>
public record struct RotationSpeed3D(Vector3 SpeedRadPerSec);
