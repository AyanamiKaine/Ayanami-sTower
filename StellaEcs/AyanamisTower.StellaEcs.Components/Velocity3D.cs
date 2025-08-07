using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// A basic wrapper around a vec3 type, exposed as Value
/// </summary>
public struct Velocity3D(float X = 0, float Y = 0, float Z = 0)
{
    /// <summary>
    /// Access to the vector3 type
    /// </summary>
    public Vector3 Value  = new(X, Y, Z);
}

