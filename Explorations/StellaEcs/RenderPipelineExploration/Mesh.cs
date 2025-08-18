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
        // Keep legacy size of 2 units per axis (matching previous +/-1 extents)
        return CreateBox3D(2f, 2f, 2f);
    }

    /// <summary>
    /// Creates a 3D box mesh with explicit size on each axis.
    /// </summary>
    /// <param name="sizeX">Full width along X.</param>
    /// <param name="sizeY">Full height along Y.</param>
    /// <param name="sizeZ">Full depth along Z.</param>
    public static Mesh CreateBox3D(float sizeX = 1.0f, float sizeY = 1.0f, float sizeZ = 1.0f)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeX);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeY);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeZ);

        float hx = sizeX * 0.5f;
        float hy = sizeY * 0.5f;
        float hz = sizeZ * 0.5f;

        var vertices = new Vertex[]
        {
            new(new(-hx,-hy, hz), Vector3.Normalize(new(-hx,-hy, hz)), new(1,0,0)),
            new(new( hx,-hy, hz), Vector3.Normalize(new( hx,-hy, hz)), new(0,1,0)),
            new(new( hx, hy, hz), Vector3.Normalize(new( hx, hy, hz)), new(0,0,1)),
            new(new(-hx, hy, hz), Vector3.Normalize(new(-hx, hy, hz)), new(1,1,0)),
            new(new(-hx,-hy,-hz), Vector3.Normalize(new(-hx,-hy,-hz)), new(1,0,1)),
            new(new( hx,-hy,-hz), Vector3.Normalize(new( hx,-hy,-hz)), new(0,1,1)),
            new(new( hx, hy,-hz), Vector3.Normalize(new( hx, hy,-hz)), new(1,1,1)),
            new(new(-hx, hy,-hz), Vector3.Normalize(new(-hx, hy,-hz)), new(0.2f,0.2f,0.2f)),
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

    /// <summary>
    /// Creates a 3D sphere/ellipsoid mesh centered at the origin.
    /// </summary>
    /// <param name="radius">Radius on X and Z axes.</param>
    /// <param name="height">Full height on Y axis. If equal to <paramref name="radius"/>*2, it's a sphere.</param>
    /// <param name="radialSegments">Number of segments around Y (longitude). Minimum 3.</param>
    /// <param name="rings">Number of rings from bottom to top (latitude). Minimum 2.</param>
    public static Mesh CreateSphere3D(float radius = 0.5f, float height = 1.0f, int radialSegments = 64, int rings = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(radialSegments, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(rings, 2);

        var a = radius;         // X/Z radius
        var b = height * 0.5f;  // Y radius

        int cols = radialSegments + 1; // duplicate seam
        int rows = rings + 1;          // include poles as rows 0 and rings

        var vertices = new Vertex[cols * rows];
        var indices = new uint[radialSegments * rings * 6];

        int vi = 0;
        for (int r = 0; r <= rings; r++)
        {
            // v in [0,1], theta from 0 (top) to pi (bottom)
            float v = (float)r / rings;
            float theta = v * MathF.PI; // latitude

            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int s = 0; s <= radialSegments; s++)
            {
                // u in [0,1], phi from 0..2pi
                float u = (float)s / radialSegments;
                float phi = u * MathF.Tau; // longitude

                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                // Ellipsoid position
                float x = a * sinTheta * cosPhi;
                float y = b * cosTheta;
                float z = a * sinTheta * sinPhi;

                // Correct ellipsoid normal: (x/a^2, y/b^2, z/c^2) normalized, with c=a
                var n = new Vector3(
                    x / (a * a),
                    y / (b * b),
                    z / (a * a)
                );
                n = Vector3.Normalize(n);

                // Color from normal (visual aid)
                var color = 0.5f * (n + Vector3.One);

                vertices[vi++] = new Vertex(new Vector3(x, y, z), n, color);
            }
        }

        int ii = 0;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < radialSegments; s++)
            {
                int a0 = (r * cols) + s;
                int a1 = a0 + 1;
                int b0 = ((r + 1) * cols) + s;
                int b1 = b0 + 1;

                // Two triangles per quad, CCW winding
                indices[ii++] = (uint)a0;
                indices[ii++] = (uint)b0;
                indices[ii++] = (uint)a1;

                indices[ii++] = (uint)a1;
                indices[ii++] = (uint)b0;
                indices[ii++] = (uint)b1;
            }
        }

        return new Mesh { Vertices = vertices, Indices = indices };
    }
}
