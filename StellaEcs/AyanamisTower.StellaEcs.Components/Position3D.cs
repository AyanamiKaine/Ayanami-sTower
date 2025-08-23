using System.Numerics;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// A basic wrapper around a vec3 type, exposed as Value
/// </summary>
public struct Position3D
{
    /// <summary>
    /// Access to the vector3 type
    /// </summary>
    public Vector3Double Value = Vector3Double.Zero;
    /// <summary>
    /// Creates a new Position3D from a Vector3Double.
    /// </summary>
    /// <param name="v"></param>
    public Position3D(Vector3Double v) { Value = v; }
    /// <summary>
    /// Creates a new Position3D from individual coordinates.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Position3D(double x, double y, double z) { Value = new Vector3Double(x, y, z); }
    /// <summary>
    /// Creates a new Position3D from individual coordinates.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Position3D(float x, float y, float z) { Value = new Vector3Double(x, y, z); }

}
