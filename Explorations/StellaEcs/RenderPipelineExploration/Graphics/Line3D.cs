using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// Represents a 3D line segment.
/// </summary>
public struct Line3D
{
    /// <summary>
    /// The starting point of the line segment.
    /// </summary>
    public Vector3Double Start { get; set; }
    /// <summary>
    /// The ending point of the line segment.
    /// </summary>
    public Vector3Double End { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Line3D"/> class.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public Line3D(Vector3Double start, Vector3Double end)
    {
        Start = start;
        End = end;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Line3D"/> class.
    /// </summary>
    public Line3D()
    {
        Start = Vector3Double.Zero;
        End = Vector3Double.Zero;
    }
}
