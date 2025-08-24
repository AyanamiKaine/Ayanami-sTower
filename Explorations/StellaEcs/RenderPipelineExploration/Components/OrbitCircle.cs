using System;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;

/// <summary>
/// Component that requests an orbit circle be drawn around a parent entity.
/// The circle is drawn in the XZ plane at the parent's position with the given radius.
/// </summary>
public struct OrbitCircle
{
    /// <summary>
    /// The parent entity around which the orbit circle is drawn.
    /// </summary>
    public Entity Parent;
    /// <summary>
    /// Radius of the orbit circle.
    /// </summary>
    public double Radius;
    /// <summary>
    /// Color of the orbit circle.
    /// </summary>
    public Color Color;
    /// <summary>
    /// Number of segments used to draw the orbit circle.
    /// </summary>
    public int Segments;
    /// <summary>
    /// Creates a new OrbitCircle component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="radius"></param>
    /// <param name="color"></param>
    /// <param name="segments"></param>
    public OrbitCircle(Entity parent, double radius, Color color, int segments = 64)
    {
        Parent = parent;
        Radius = radius;
        Color = color;
        Segments = Math.Max(4, segments);
    }

    /// <summary>
    /// Creates a new OrbitCircle component.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="radius"></param>
    /// <param name="color"></param>
    /// <param name="segments"></param>
    public OrbitCircle(Entity parent, float radius, Color color, int segments = 64)
    {
        Parent = parent;
        Radius = radius;
        Color = color;
        Segments = Math.Max(4, segments);
    }
}
