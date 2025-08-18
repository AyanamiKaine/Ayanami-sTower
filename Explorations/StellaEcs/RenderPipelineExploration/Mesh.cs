using System;
using System.Numerics;

namespace RenderPipelineExploration;

/// <summary>
/// Represents a 3D mesh.
/// </summary>
public struct Mesh
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

    /// <summary>
    /// Creates a simple XY plane centered at the origin with +Z normal.
    /// </summary>
    /// <param name="sizeX">Full width along X.</param>
    /// <param name="sizeY">Full height along Y.</param>
    public static Mesh CreatePlane3D(float sizeX = 2.0f, float sizeY = 2.0f)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeX);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeY);

        float hx = sizeX * 0.5f;
        float hy = sizeY * 0.5f;

        var n = new Vector3(0, 0, 1);
        var vertices = new Vertex[]
        {
            new(new(-hx,-hy, 0), n, new(1,0,0)),
            new(new( hx,-hy, 0), n, new(0,1,0)),
            new(new( hx, hy, 0), n, new(0,0,1)),
            new(new(-hx, hy, 0), n, new(1,1,0)),
        };

        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };

        return new Mesh { Vertices = vertices, Indices = indices };
    }

    /// <summary>
    /// Creates a torus mesh centered at the origin.
    /// innerRadius/outerRadius define the inner/outer surface distances from the origin in the XZ plane.
    /// </summary>
    /// <param name="innerRadius">Distance from origin to inner tube surface (R - r). Must be greater than 0 and less than <paramref name="outerRadius"/>.</param>
    /// <param name="outerRadius">Distance from origin to outer tube surface (R + r). Must be greater than <paramref name="innerRadius"/>.</param>
    /// <param name="rings">Segments around the main ring (major circle).</param>
    /// <param name="ringSegments">Segments around the tube cross-section (minor circle).</param>
    public static Mesh CreateTorus3D(float innerRadius = 0.5f, float outerRadius = 1.0f, int rings = 64, int ringSegments = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(innerRadius);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outerRadius);
        if (outerRadius <= innerRadius) throw new ArgumentOutOfRangeException(nameof(outerRadius), "outerRadius must be greater than innerRadius.");
        ArgumentOutOfRangeException.ThrowIfLessThan(rings, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(ringSegments, 3);

        // Derive major/minor radii: R = (outer+inner)/2, r = (outer-inner)/2
        float R = 0.5f * (outerRadius + innerRadius);
        float r = 0.5f * (outerRadius - innerRadius);

        int cols = rings + 1;         // duplicate seam around major ring
        int rows = ringSegments + 1;  // duplicate seam around minor circle

        var vertices = new Vertex[cols * rows];
        var indices = new uint[rings * ringSegments * 6];

        int vi = 0;
        for (int i = 0; i <= rings; i++)
        {
            float u = (float)i / rings;           // [0,1]
            float phi = u * MathF.Tau;            // around Y axis (major angle)
            float cosU = MathF.Cos(phi);
            float sinU = MathF.Sin(phi);

            // Direction from Y axis in XZ plane
            var dir = new Vector3(cosU, 0, sinU);
            var up = Vector3.UnitY;

            for (int j = 0; j <= ringSegments; j++)
            {
                float v = (float)j / ringSegments; // [0,1]
                float theta = v * MathF.Tau;        // around tube
                float cosV = MathF.Cos(theta);
                float sinV = MathF.Sin(theta);

                // Centerline point on major circle
                var center = dir * R; // (R cosU, 0, R sinU)

                // Local frame: dir (radial in XZ), up (Y). Tube offset
                var offset = ((cosV * dir) + (sinV * up)) * r;
                var pos = center + offset;

                // Normal is offset normalized (independent of r)
                var normal = Vector3.Normalize((cosV * dir) + (sinV * up));
                var color = 0.5f * (normal + Vector3.One);

                vertices[vi++] = new Vertex(pos, normal, color);
            }
        }

        int ii = 0;
        for (int i = 0; i < rings; i++)
        {
            for (int j = 0; j < ringSegments; j++)
            {
                int a0 = (i * rows) + j;
                int a1 = a0 + 1;
                int b0 = ((i + 1) * rows) + j;
                int b1 = b0 + 1;

                // CCW winding (a0 -> a1 -> b0), (a1 -> b1 -> b0)
                indices[ii++] = (uint)a0;
                indices[ii++] = (uint)a1;
                indices[ii++] = (uint)b0;

                indices[ii++] = (uint)a1;
                indices[ii++] = (uint)b1;
                indices[ii++] = (uint)b0;
            }
        }

        return new Mesh { Vertices = vertices, Indices = indices };
    }

    /// <summary>
    /// Creates a cylinder/frustum mesh with optional differing top and bottom radii, including caps.
    /// </summary>
    /// <param name="topRadius">Radius at the top cap (y = +height/2). Can be 0 for a cone tip.</param>
    /// <param name="bottomRadius">Radius at the bottom cap (y = -height/2). Can be 0 for a cone tip.</param>
    /// <param name="height">Full height of the cylinder.</param>
    /// <param name="radialSegments">Number of segments around the circumference. Minimum 3.</param>
    /// <param name="rings">Number of vertical divisions along the side. Minimum 1.</param>
    public static Mesh CreateCylinder3D(float topRadius = 0.5f, float bottomRadius = 0.5f, float height = 1.0f, int radialSegments = 64, int rings = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topRadius);
        ArgumentOutOfRangeException.ThrowIfNegative(bottomRadius);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(radialSegments, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(rings, 1);
        if (topRadius == 0 && bottomRadius == 0) throw new ArgumentOutOfRangeException(nameof(topRadius), "At least one radius must be > 0.");

        int cols = radialSegments + 1; // duplicate seam
        int rows = rings + 1;          // include top and bottom rows

        int sideVertCount = cols * rows;
        int capRimCount = radialSegments + 1; // duplicate seam
        int topVertCount = capRimCount + 1;   // rim + center
        int botVertCount = capRimCount + 1;   // rim + center
        int totalVerts = sideVertCount + topVertCount + botVertCount;

        var vertices = new Vertex[totalVerts];

        // Indices: sides + caps
        int sideIndexCount = radialSegments * rings * 6;
        int capIndexCount = radialSegments * 3 * 2; // top + bottom
        var indices = new uint[sideIndexCount + capIndexCount];

        // Precompute slope for side normals
        float rPrime = bottomRadius - topRadius; // dr/dt over t in [0,1]
        float h = height; // for clarity

        // Build side vertices
        int vi = 0;
        for (int r = 0; r <= rings; r++)
        {
            float t = (float)r / rings; // 0 = top, 1 = bottom
            float y = (0.5f - t) * h;
            float radius = topRadius + rPrime * t;
            for (int s = 0; s <= radialSegments; s++)
            {
                float u = (float)s / radialSegments;
                float phi = u * MathF.Tau;
                float cos = MathF.Cos(phi);
                float sin = MathF.Sin(phi);

                var pos = new Vector3(radius * cos, y, radius * sin);
                // Side normal for frustum: normalize((h*cos, r', h*sin))
                var n = Vector3.Normalize(new Vector3(h * cos, rPrime, h * sin));
                var color = 0.5f * (n + Vector3.One);
                vertices[vi++] = new Vertex(pos, n, color);
            }
        }

        // Build caps vertices
        int topStart = vi;
        float yTop = +0.5f * h;
        for (int s = 0; s <= radialSegments; s++)
        {
            float u = (float)s / radialSegments;
            float phi = u * MathF.Tau;
            float cos = MathF.Cos(phi);
            float sin = MathF.Sin(phi);
            var pos = new Vector3(topRadius * cos, yTop, topRadius * sin);
            var n = Vector3.UnitY;
            var color = 0.5f * (n + Vector3.One);
            vertices[vi++] = new Vertex(pos, n, color);
        }
        int topCenterIndex = vi;
        vertices[vi++] = new Vertex(new Vector3(0, yTop, 0), Vector3.UnitY, new Vector3(1, 1, 1));

        int botStart = vi;
        float yBot = -0.5f * h;
        for (int s = 0; s <= radialSegments; s++)
        {
            float u = (float)s / radialSegments;
            float phi = u * MathF.Tau;
            float cos = MathF.Cos(phi);
            float sin = MathF.Sin(phi);
            var pos = new Vector3(bottomRadius * cos, yBot, bottomRadius * sin);
            var n = -Vector3.UnitY;
            var color = 0.5f * (n + Vector3.One);
            vertices[vi++] = new Vertex(pos, n, color);
        }
        int botCenterIndex = vi;
        vertices[vi++] = new Vertex(new Vector3(0, yBot, 0), -Vector3.UnitY, new Vector3(1, 1, 1));

        // Indices - sides
        int ii = 0;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < radialSegments; s++)
            {
                int a0 = r * cols + s;
                int a1 = a0 + 1;
                int b0 = (r + 1) * cols + s;
                int b1 = b0 + 1;

                // CCW: (a0, a1, b0), (a1, b1, b0)
                indices[ii++] = (uint)a0;
                indices[ii++] = (uint)a1;
                indices[ii++] = (uint)b0;

                indices[ii++] = (uint)a1;
                indices[ii++] = (uint)b1;
                indices[ii++] = (uint)b0;
            }
        }

        // Indices - top cap (CCW when viewed from above)
        for (int s = 0; s < radialSegments; s++)
        {
            int a = topStart + s;
            int b = topStart + s + 1;
            indices[ii++] = (uint)topCenterIndex;
            indices[ii++] = (uint)b; // Swapped
            indices[ii++] = (uint)a; // Swapped
        }

        // Indices - bottom cap
        for (int s = 0; s < radialSegments; s++)
        {
            int a = botStart + s;
            int b = botStart + s + 1;
            indices[ii++] = (uint)botCenterIndex;
            indices[ii++] = (uint)a; // Swapped
            indices[ii++] = (uint)b; // Swapped
        }

        return new Mesh { Vertices = vertices, Indices = indices };
    }

    // -----------------------------
    // Mesh helpers (non-destructive)
    // -----------------------------

    /// <summary>
    /// Applies a transform to the mesh positions and transforms normals using the inverse-transpose of the matrix.
    /// Returns a new Mesh; the original is not modified.
    /// </summary>
    public Mesh Transform(Matrix4x4 transform)
    {
        var verts = new Vertex[Vertices.Length];
        // Compute inverse-transpose for normal transform; if non-invertible, fallback to transform.
        Matrix4x4 normalMat;
        if (Matrix4x4.Invert(transform, out var inv))
        {
            normalMat = Matrix4x4.Transpose(inv);
        }
        else
        {
            normalMat = transform;
        }

        for (int i = 0; i < Vertices.Length; i++)
        {
            var v = Vertices[i];
            var pos = Vector3.Transform(v.Position, transform);
            var nrm = Vector3.TransformNormal(v.Normal, normalMat);
            if (nrm != Vector3.Zero) nrm = Vector3.Normalize(nrm);
            verts[i] = new Vertex(pos, nrm, v.Color);
        }
        return new Mesh { Vertices = verts, Indices = (uint[])Indices.Clone() };
    }

    /// <summary>
    /// Returns a new mesh scaled uniformly.
    /// </summary>
    public Mesh Scale(float uniform)
    {
        return Transform(Matrix4x4.CreateScale(uniform));
    }

    /// <summary>
    /// Returns a new mesh scaled non-uniformly per-axis.
    /// </summary>
    public Mesh Scale(Vector3 scale)
    {
        return Transform(Matrix4x4.CreateScale(scale));
    }

    /// <summary>
    /// Returns a new mesh translated by the given offset.
    /// </summary>
    public Mesh Translate(Vector3 offset)
    {
        return Transform(Matrix4x4.CreateTranslation(offset));
    }

    /// <summary>
    /// Returns a new mesh rotated by the given quaternion.
    /// </summary>
    public Mesh Rotate(Quaternion rotation)
    {
        return Transform(Matrix4x4.CreateFromQuaternion(rotation));
    }

    /// <summary>
    /// Returns a new mesh with triangle winding inverted. Optionally flips normals.
    /// </summary>
    public Mesh InvertWinding(bool flipNormals = true)
    {
        var inds = (uint[])Indices.Clone();
        for (int i = 0; i + 2 < inds.Length; i += 3)
        {
            (inds[i + 1], inds[i + 2]) = (inds[i + 2], inds[i + 1]);
        }

        var verts = new Vertex[Vertices.Length];
        if (flipNormals)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                var v = Vertices[i];
                verts[i] = new Vertex(v.Position, -v.Normal, v.Color);
            }
        }
        else
        {
            Array.Copy(Vertices, verts, verts.Length);
        }

        return new Mesh { Vertices = verts, Indices = inds };
    }

    /// <summary>
    /// Computes axis-aligned bounds of the mesh.
    /// </summary>
    public (Vector3 Min, Vector3 Max) ComputeBounds()
    {
        if (Vertices.Length == 0) return (Vector3.Zero, Vector3.Zero);
        var min = Vertices[0].Position;
        var max = Vertices[0].Position;
        for (int i = 1; i < Vertices.Length; i++)
        {
            var p = Vertices[i].Position;
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return (min, max);
    }

    /// <summary>
    /// Recenters the mesh so its AABB center is at the origin.
    /// </summary>
    public Mesh RecenterToOrigin()
    {
        var (min, max) = ComputeBounds();
        var center = 0.5f * (min + max);
        return Translate(-center);
    }

    /// <summary>
    /// Combines multiple meshes into one. Indices are adjusted appropriately.
    /// </summary>
    public static Mesh Combine(params Mesh[] meshes)
    {
        if (meshes == null || meshes.Length == 0)
        {
            return new Mesh { Vertices = Array.Empty<Vertex>(), Indices = Array.Empty<uint>() };
        }

        int totalVerts = 0;
        int totalInds = 0;
        foreach (var m in meshes)
        {
            totalVerts += m.Vertices.Length;
            totalInds += m.Indices.Length;
        }

        var verts = new Vertex[totalVerts];
        var inds = new uint[totalInds];

        int vOff = 0;
        int iOff = 0;
        foreach (var m in meshes)
        {
            Array.Copy(m.Vertices, 0, verts, vOff, m.Vertices.Length);
            for (int i = 0; i < m.Indices.Length; i++)
            {
                inds[iOff + i] = (uint)(m.Indices[i] + vOff);
            }
            vOff += m.Vertices.Length;
            iOff += m.Indices.Length;
        }

        return new Mesh { Vertices = verts, Indices = inds };
    }
}
