using System;
using System.Numerics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Represents the size 3D of an entity using a Vector3.
/// </summary>
/// <param name="Value">The size value.</param>
public record struct Size3D(Vector3 Value);
