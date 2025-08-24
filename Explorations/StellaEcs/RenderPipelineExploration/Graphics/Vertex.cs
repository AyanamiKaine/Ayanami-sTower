using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;


/// <summary>
/// Represents a vertex in 3D space.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vertex : IVertexType
{
    /// <summary>
    /// The position of the vertex.
    /// </summary>
    public Vector3 Position;
    /// <summary>
    /// The normal vector of the vertex.
    /// </summary>
    public Vector3 Normal;
    /// <summary>
    /// The color of the vertex.
    /// </summary>
    public Vector3 Color;
    /// <summary>
    /// Texture coordinates.
    /// </summary>
    public Vector2 UV;

    /// <summary>
    /// Creates a new vertex.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="normal"></param>
    /// <param name="color"></param>
    /// <param name="uv"></param>
    public Vertex(Vector3 position, Vector3 normal, Vector3 color, Vector2 uv = default)
    { Position = position; Normal = normal; Color = color; UV = uv; }

    /// <summary>
    /// The formats of the vertex elements.
    /// </summary>
    public static VertexElementFormat[] Formats =>
        [VertexElementFormat.Float3, VertexElementFormat.Float3, VertexElementFormat.Float3, VertexElementFormat.Float2];

    /// <summary>
    /// The offsets of the vertex elements.
    /// </summary>
    public static uint[] Offsets => [0u, 12u, 24u, 36u];
}
