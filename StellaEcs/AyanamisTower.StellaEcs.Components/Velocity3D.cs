using System;
using System.Numerics;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// A basic wrapper around a vec3 type, exposed as Value
/// </summary>
public struct Velocity3D(double X = 0, double Y = 0, double Z = 0)
{
    /// <summary>
    /// Access to the vector3 type
    /// </summary>
    public Vector3Double Value = new(X, Y, Z);
    /// <summary>
    /// Creates a new instance of the <see cref="Velocity3D"/> struct from a <see cref="Vector3Double"/> instance.
    /// </summary>
    /// <param name="v"></param>
    public Velocity3D(Vector3Double v) : this(v.X, v.Y, v.Z) { }
    /// <summary>
    /// Creates a new instance of the <see cref="Velocity3D"/> struct from individual components.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Velocity3D(float x, float y, float z) : this((double)x, (double)y, (double)z) { }
}

