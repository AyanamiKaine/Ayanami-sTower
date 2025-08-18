using System;
using System.Numerics;

namespace RenderPipelineExploration;

/// <summary>
/// Represents a 3D mesh.
/// </summary>
public class Mesh
{
    /// <summary>
    /// Vertex data for the mesh
    /// </summary>
    public required Vertex[] Vertices { get; set; }

    /// <summary>
    /// Index data for the mesh
    /// </summary>
    public required uint[] Indices { get; set; }

    /// <summary>
    /// Creates a 3D box mesh.
    /// </summary>
    /// <returns></returns>
    public static Mesh CreateBox3D()
    {
        var vertices = new Vertex[]
        {
            new(new(-1,-1, 1), Vector3.Normalize(new(-1,-1, 1)), new(1,0,0)),
            new(new( 1,-1, 1), Vector3.Normalize(new( 1,-1, 1)), new(0,1,0)),
            new(new( 1, 1, 1), Vector3.Normalize(new( 1, 1, 1)), new(0,0,1)),
            new(new(-1, 1, 1), Vector3.Normalize(new(-1, 1, 1)), new(1,1,0)),
            new(new(-1,-1,-1), Vector3.Normalize(new(-1,-1,-1)), new(1,0,1)),
            new(new( 1,-1,-1), Vector3.Normalize(new( 1,-1,-1)), new(0,1,1)),
            new(new( 1, 1,-1), Vector3.Normalize(new( 1, 1,-1)), new(1,1,1)),
            new(new(-1, 1,-1), Vector3.Normalize(new(-1, 1,-1)), new(0.2f,0.2f,0.2f)),
        };

        var indices = new uint[]
        {
                // Front
                0,1,2, 0,2,3,
                // Right
                1,5,6, 1,6,2,
                // Back
                5,4,7, 5,7,6,
                // Left
                4,0,3, 4,3,7,
                // Bottom
                4,5,1, 4,1,0,
                // Top
                3,2,6, 3,6,7
        };

        return new Mesh { Vertices = vertices, Indices = indices };
    }
}
