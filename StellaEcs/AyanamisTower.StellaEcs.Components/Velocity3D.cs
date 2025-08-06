using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// A basic wrapper around a vec3 type, exposed as Value
/// </summary>
public struct Velocity3D(int X = 0, int Y = 0, int Z = 0)
{
    /// <summary>
    /// Access to the vector3 type
    /// </summary>
    public Vector3 Value = new(X, Y, Z);
}

