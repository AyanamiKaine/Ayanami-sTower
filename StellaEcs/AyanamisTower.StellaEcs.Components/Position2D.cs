using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// A basic wrapper around a vec2 type, exposed as Value
/// </summary>
public struct Position2D(float X = 0, float Y = 0)
{
    /// <summary>
    /// Access to the vector2 type
    /// </summary>
    public Vector2 Value  = new(X, Y);
}

