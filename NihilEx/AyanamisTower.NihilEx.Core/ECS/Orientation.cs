using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents the orientation of an entity using a Quaternion.
/// Quaternions are generally preferred over Euler angles for 3D rotation
/// as they avoid gimbal lock and provide smooth interpolation.
/// </summary>
public record struct Orientation(Quaternion Value);
