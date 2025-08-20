using System;
using System.Numerics;

namespace StellaInvicta;

/// <summary>
/// Represents a 3D line segment.
/// </summary>
public struct Line3D
{
    /// <summary>
    /// The starting point of the line segment.
    /// </summary>
    public Vector3 Start { get; set; }
    /// <summary>
    /// The ending point of the line segment.
    /// </summary>
    public Vector3 End { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Line3D"/> class.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public Line3D(Vector3 start, Vector3 end)
    {
        Start = start;
        End = end;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Line3D"/> class.
    /// </summary>
    public Line3D()
    {
        Start = Vector3.Zero;
        End = Vector3.Zero;
    }
}
