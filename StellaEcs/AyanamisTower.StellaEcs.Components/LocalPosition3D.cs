using System.Numerics;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Local space position relative to the entity's Parent. This is not a world-space position.
/// </summary>
public struct LocalPosition3D
{
    /// <summary>
    /// Creates a new instance of the <see cref="LocalPosition3D"/> struct.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public LocalPosition3D(double x, double y, double z)
    {
        Value = new Vector3Double(x, y, z);
    }

    /// <summary>
    /// Access to the vector3 type representing local offset from parent.
    /// </summary>
    public Vector3Double Value = Vector3Double.Zero;
    /// <summary>
    /// Creates a new instance of the <see cref="LocalPosition3D"/> struct.
    /// </summary>
    /// <param name="v"></param>
    public LocalPosition3D(Vector3Double v) { Value = v; }
}
