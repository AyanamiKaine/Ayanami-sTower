using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Per-axis angular velocity in radians per second.
/// X = pitch rate, Y = yaw rate, Z = roll rate.
/// </summary>
public struct AngularVelocity3D
{
    /// <summary>
    /// The angular velocity vector.
    /// </summary>
    public Vector3 Value;
    /// <summary>
    /// Creates a new instance of the <see cref="AngularVelocity3D"/> struct.
    /// </summary>
    /// <param name="v"></param>
    public AngularVelocity3D(Vector3 v) { Value = v; }
    /// <summary>
    /// Creates a new instance of the <see cref="AngularVelocity3D"/> struct.
    /// </summary>
    /// <param name="pitch"></param>
    /// <param name="yaw"></param>
    /// <param name="roll"></param>
    public AngularVelocity3D(float pitch, float yaw, float roll) { Value = new Vector3(pitch, yaw, roll); }
}
