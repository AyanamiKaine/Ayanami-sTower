using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Local space position relative to the entity's Parent. This is not a world-space position.
/// </summary>
public struct LocalPosition3D(float X = 0, float Y = 0, float Z = 0)
{
    /// <summary>
    /// Access to the vector3 type representing local offset from parent.
    /// </summary>
    public Vector3 Value = new(X, Y, Z);
}
