using System;
using System.Numerics;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Graphics;

/// <summary>
/// Simple mesh abstraction for static shapes
/// </summary>
/// <param name="vertexBuffer"></param>
/// <param name="vertexCount"></param>
/// <param name="primitiveType"></param>
/// <param name="indexBuffer"></param>
/// <param name="indexCount"></param>
public class Mesh(MoonWorks.Graphics.Buffer vertexBuffer, int vertexCount, PrimitiveType primitiveType, MoonWorks.Graphics.Buffer? indexBuffer = null, int indexCount = 0) : IDisposable
{
    /// <summary>
    /// Vertex struct for 3D mesh generation
    /// </summary>
    /// <remarks>
    /// Creates a new Vertex3D.
    /// </remarks>
    /// <param name="pos"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    public struct Vertex3D(Vector3 pos, float r, float g, float b)
    {
        /// <summary>
        /// Position of the vertex in 3D space.
        /// </summary>
        public Vector3 Pos = pos;
        /// <summary>
        /// Red color component.
        /// </summary>
        public float R = r, G = g, B = b;
    }

    /// <summary>
    /// Vertex struct for lit 3D meshes (pos, normal, color/albedo)
    /// </summary>
    public struct Vertex3DLit(Vector3 pos, Vector3 nrm, float r, float g, float b)
    {
        /// <summary>Position of the vertex in 3D space.</summary>
        public Vector3 Pos = pos;
        /// <summary>Surface normal in 3D space.</summary>
        public Vector3 Nrm = nrm;
        /// <summary>Albedo color components.</summary>
        public float R = r, G = g, B = b;
    }

    /// <summary>
    /// Vertex struct for lit textured 3D meshes (pos, normal, uv)
    /// </summary>
    public struct Vertex3DTexturedLit(Vector3 pos, Vector3 nrm, Vector2 uv)
    {
        /// <summary>Position of the vertex in 3D space.</summary>
        public Vector3 Pos = pos;
        /// <summary>Surface normal in 3D space.</summary>
        public Vector3 Nrm = nrm;
        /// <summary>Texture coordinates (UV).</summary>
        public Vector2 UV = uv;
    }

    /// <summary>
    /// Superset vertex for 3D materials: position, normal, UV, and color (albedo).
    /// Pipelines can ignore unused attributes.
    /// Order in memory: Pos, Nrm, UV, Col.
    /// </summary>
    public struct Vertex3DMaterial(Vector3 pos, Vector3 nrm, Vector2 uv, Vector3 col)
    {
        /// <summary>Position of the vertex in 3D space.</summary>
        public Vector3 Pos = pos;
        /// <summary>Surface normal in 3D space.</summary>
        public Vector3 Nrm = nrm;
        /// <summary>Texture coordinates (UV).</summary>
        public Vector2 UV = uv;
        /// <summary>Albedo color (RGB).</summary>
        public Vector3 Col = col;
    }

    /// <summary>
    /// Creates a colored cube mesh centered at origin with given size.
    /// </summary>
    public static Mesh CreateBox3D(GraphicsDevice device, float size)
    {
        float h = size / 2f;
        // 8 corners
        var verts = new Vertex3D[]
        {
                new(new Vector3(-h, -h, -h), 1, 0, 0), // 0
                new(new Vector3( h, -h, -h), 0, 1, 0), // 1
                new(new Vector3( h,  h, -h), 0, 0, 1), // 2
                new(new Vector3(-h,  h, -h), 1, 1, 0), // 3
                new(new Vector3(-h, -h,  h), 1, 0, 1), // 4
                new(new Vector3( h, -h,  h), 0, 1, 1), // 5
                new(new Vector3( h,  h,  h), 1, 1, 1), // 6
                new(new Vector3(-h,  h,  h), 0, 0, 0), // 7
        };
        // 12 triangles (2 per face), CCW winding for outward faces
        var indices = new uint[] {
                // -Z (back)
                0,2,1, 0,3,2,
                // +Z (front)
                4,5,6, 4,6,7,
                // -Y (bottom)
                0,1,5, 0,5,4,
                // +Y (top)
                3,6,2, 3,7,6,
                // -X (left)
                0,7,3, 0,4,7,
                // +X (right)
                1,2,6, 1,6,5
            };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3D>(device, "BoxVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "BoxIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex3D>(device, "BoxVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex3D>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "BoxIBUpload", TransferBufferUsage.Upload, (uint)indices.Length);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        // Dispose upload resources
        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Length);
    }

    /// <summary>
    /// Creates a flat-colored, unlit cube mesh centered at origin with given size and color.
    /// This version uses the simple Vertex3D layout for the unlit pipeline.
    /// </summary>
    public static Mesh CreateBox3DColored(GraphicsDevice device, float size, Vector3 color)
    {
        float h = size / 2f;
        var c = color;
        // 8 corners, all with the same color
        var verts = new Vertex3D[]
        {
                new(new Vector3(-h, -h, -h), c.X, c.Y, c.Z), // 0
                new(new Vector3( h, -h, -h), c.X, c.Y, c.Z), // 1
                new(new Vector3( h,  h, -h), c.X, c.Y, c.Z), // 2
                new(new Vector3(-h,  h, -h), c.X, c.Y, c.Z), // 3
                new(new Vector3(-h, -h,  h), c.X, c.Y, c.Z), // 4
                new(new Vector3( h, -h,  h), c.X, c.Y, c.Z), // 5
                new(new Vector3( h,  h,  h), c.X, c.Y, c.Z), // 6
                new(new Vector3(-h,  h,  h), c.X, c.Y, c.Z), // 7
        };
        var indices = new uint[] {
                // -Z (back)
                0,2,1, 0,3,2,
                // +Z (front)
                4,5,6, 4,6,7,
                // -Y (bottom)
                0,1,5, 0,5,4,
                // +Y (top)
                3,6,2, 3,7,6,
                // -X (left)
                0,7,3, 0,4,7,
                // +X (right)
                1,2,6, 1,6,5
            };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3D>(device, "BoxVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "BoxIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex3D>(device, "BoxVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex3D>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "BoxIBUpload", TransferBufferUsage.Upload, (uint)indices.Length);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Length);
    }

    /// <summary>
    /// Generates a UV sphere with equirectangular mapping suitable for planet textures.
    /// </summary>
    /// <param name="device">Graphics device.</param>
    /// <param name="radius">Sphere radius.</param>
    /// <param name="slices">Number of longitudinal segments (around the equator).</param>
    /// <param name="stacks">Number of latitudinal segments (from pole to pole).</param>
    /// <returns>A mesh with <see cref="Vertex3DTexturedLit"/> vertices and indexed triangles.</returns>
    public static Mesh CreateSphere3DTexturedLit(GraphicsDevice device, float radius, int slices = 64, int stacks = 32)
    {
        slices = Math.Max(3, slices);
        stacks = Math.Max(2, stacks);

        // We duplicate the seam at u=0/1 by adding an extra column of vertices.
        int vertsPerRow = slices + 1;
        int vertCount = vertsPerRow * (stacks + 1);
        var verts = new Vertex3DMaterial[vertCount];

        int vi = 0;
        for (int y = 0; y <= stacks; y++)
        {
            float v = (float)y / stacks;                 // 0..1 from north(0) to south(1)
            float theta = v * MathF.PI;                  // 0..PI
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int x = 0; x <= slices; x++)
            {
                float u = (float)x / slices;            // 0..1 wraps around
                float phi = u * MathF.Tau;               // 0..2PI
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                var nx = sinTheta * cosPhi;
                var ny = cosTheta;
                var nz = sinTheta * sinPhi;

                var n = new Vector3(nx, ny, nz);
                var p = n * radius;
                verts[vi++] = new Vertex3DMaterial(p, Vector3.Normalize(n), new Vector2(u, 1f - v), new Vector3(1f, 1f, 1f));
            }
        }

        // Indices (two triangles per quad on the grid)
        int quadsX = slices;
        int quadsY = stacks;
        var indices = new List<uint>(quadsX * quadsY * 6);
        for (int y = 0; y < stacks; y++)
        {
            for (int x = 0; x < slices; x++)
            {
                int i0 = (y * vertsPerRow) + x;       // top-left
                int i1 = i0 + 1;                       // top-right
                int i2 = i0 + vertsPerRow;             // bottom-left
                int i3 = i2 + 1;                       // bottom-right

                // CCW winding when viewed from outside the sphere
                indices.Add((uint)i0);
                indices.Add((uint)i1);
                indices.Add((uint)i2);

                indices.Add((uint)i2);
                indices.Add((uint)i1);
                indices.Add((uint)i3);
            }
        }

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DMaterial>(device, "SphereTexturedLitVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "SphereTexturedLitIB", BufferUsageFlags.Index, (uint)indices.Count);

        var vtransfer = TransferBuffer.Create<Vertex3DMaterial>(device, "SphereTexturedLitVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex3DMaterial>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "SphereTexturedLitIBUpload", TransferBufferUsage.Upload, (uint)indices.Count);
        var ispan = itransfer.Map<uint>(cycle: false);
        // Copy list to span without CollectionsMarshal to avoid extra dependency
        for (int i = 0; i < indices.Count; i++)
        {
            ispan[i] = indices[i];
        }
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Count);
    }

    /// <summary>
    /// Generic UV-sphere using the superset vertex (pos,nrm,uv,col). Suitable for both lit and textured pipelines.
    /// </summary>
    public static Mesh CreateSphere3D(GraphicsDevice device, float radius, int slices = 64, int stacks = 32)
        => CreateSphere3DTexturedLit(device, radius, slices, stacks);

    /// <summary>
    /// Generic lit icosphere using the superset vertex (pos,nrm,uv,col) with flat albedo color.
    /// Provided for ergonomic parity with CreateSphere3DLit.
    /// </summary>
    public static Mesh CreateSphere3D(GraphicsDevice device, float radius, Vector3 color, int subdivisions = 1)
        => CreateSphere3DLit(device, radius, color, subdivisions);

    /// <summary>
    /// Generic lit cube using the superset vertex (pos,nrm,uv,col) with flat albedo color.
    /// Provided for ergonomic parity with CreateSphere3D.
    /// </summary>
    public static Mesh CreateBox3D(GraphicsDevice device, float size, Vector3 color)
    {
        float h = size / 2f;
        // 24 vertices: 4 per face with face normals, ordered CCW when viewed from outside
        var verts = new Vertex3DMaterial[]
        {
            // -Z (back face) - vertices in CCW order when viewed from outside (positive Z)
            new(new Vector3(-h,-h,-h), new Vector3(0,0,-1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 0: bottom-left
            new(new Vector3(-h, h,-h), new Vector3(0,0,-1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 1: top-left  
            new(new Vector3( h, h,-h), new Vector3(0,0,-1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 2: top-right
            new(new Vector3( h,-h,-h), new Vector3(0,0,-1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 3: bottom-right
            
            // +Z (front face) - vertices in CCW order when viewed from outside (negative Z)
            new(new Vector3(-h,-h, h), new Vector3(0,0, 1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 4: bottom-left
            new(new Vector3( h,-h, h), new Vector3(0,0, 1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 5: bottom-right
            new(new Vector3( h, h, h), new Vector3(0,0, 1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 6: top-right
            new(new Vector3(-h, h, h), new Vector3(0,0, 1), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 7: top-left
            
            // -Y (bottom face) - vertices in CCW order when viewed from above (positive Y)
            new(new Vector3(-h,-h,-h), new Vector3(0,-1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 8: back-left
            new(new Vector3( h,-h,-h), new Vector3(0,-1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 9: back-right
            new(new Vector3( h,-h, h), new Vector3(0,-1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 10: front-right
            new(new Vector3(-h,-h, h), new Vector3(0,-1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 11: front-left
            
            // +Y (top face) - vertices in CCW order when viewed from below (negative Y)
            new(new Vector3(-h, h,-h), new Vector3(0, 1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 12: back-left
            new(new Vector3(-h, h, h), new Vector3(0, 1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 13: front-left
            new(new Vector3( h, h, h), new Vector3(0, 1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 14: front-right
            new(new Vector3( h, h,-h), new Vector3(0, 1,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 15: back-right
            
            // -X (left face) - vertices in CCW order when viewed from right (positive X)
            new(new Vector3(-h,-h,-h), new Vector3(-1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 16: back-bottom
            new(new Vector3(-h,-h, h), new Vector3(-1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 17: front-bottom
            new(new Vector3(-h, h, h), new Vector3(-1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 18: front-top
            new(new Vector3(-h, h,-h), new Vector3(-1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 19: back-top
            
            // +X (right face) - vertices in CCW order when viewed from left (negative X)
            new(new Vector3( h,-h,-h), new Vector3( 1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 20: back-bottom
            new(new Vector3( h, h,-h), new Vector3( 1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 21: back-top
            new(new Vector3( h, h, h), new Vector3( 1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 22: front-top
            new(new Vector3( h,-h, h), new Vector3( 1,0,0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 23: front-bottom
        };
        var indices = new uint[]
        {
            // Each face: two triangles with consistent CCW winding
            // -Z (back face)
            0,1,2, 0,2,3,
            // +Z (front face) 
            4,5,6, 4,6,7,
            // -Y (bottom face)
            8,9,10, 8,10,11,
            // +Y (top face)
            12,13,14, 12,14,15,
            // -X (left face)
            16,17,18, 16,18,19,
            // +X (right face)
            20,21,22, 20,22,23
        };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DMaterial>(device, "BoxLitVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "BoxLitIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex3DMaterial>(device, "BoxLitVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex3DMaterial>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "BoxLitIBUpload", TransferBufferUsage.Upload, (uint)indices.Length);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Length);
    }

    /// <summary>
    /// Creates a cube mesh with per-face normals for lighting and flat albedo color.
    /// </summary>
    [Obsolete("Use CreateBox3D(device, size, color) instead.")]
    public static Mesh CreateBox3DLit(GraphicsDevice device, float size, Vector3 color)
        => CreateBox3D(device, size, color);
    /// <summary>
    /// Vertex buffer for the mesh.
    /// </summary>
    public MoonWorks.Graphics.Buffer VertexBuffer { get; } = vertexBuffer;
    /// <summary>
    /// Index buffer for the mesh.
    /// </summary>
    public MoonWorks.Graphics.Buffer? IndexBuffer { get; } = indexBuffer;
    /// <summary>
    /// Number of vertices in the mesh.
    /// </summary>
    public int VertexCount { get; } = vertexCount;
    /// <summary>
    /// Number of indices in the mesh.
    /// </summary>
    public int IndexCount { get; } = indexCount;
    /// <summary>
    /// Primitive type of the mesh.
    /// </summary>
    public PrimitiveType PrimitiveType { get; } = primitiveType;

    /// <summary>
    /// Draw helper (assumes pipeline and renderPass are set up)
    /// </summary>
    public void Draw(RenderPass renderPass)
    {
        renderPass.BindVertexBuffers(VertexBuffer);
        if (IndexBuffer != null)
        {
            renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
            renderPass.DrawIndexedPrimitives((uint)IndexCount, 1, 0, 0, 0);
        }
        else
        {
            renderPass.DrawPrimitives((uint)VertexCount, 1, 0, 0);
        }
    }

    /// <summary>
    /// Vertex struct for mesh generation (matches Example shader)
    /// </summary>
    /// <remarks>
    /// Creates a new vertex.
    /// </remarks>
    /// <param name="pos"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    public struct Vertex(Vector2 pos, float r, float g, float b)
    {
        /// <summary>
        /// Position of the vertex.
        /// </summary>
        public Vector2 Pos = pos;
        /// <summary>
        /// Red color component.
        /// </summary>
        public float R = r, G = g, B = b;
    }

    /// <summary>
    /// Quad generator (XY plane, centered at origin, size)
    /// </summary>
    /// <param name="device"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static Mesh CreateQuad(GraphicsDevice device, float width, float height)
    {
        var hw = width / 2f;
        var hh = height / 2f;
        var verts = new Vertex[]
        {
                new(new Vector2(-hw, -hh), 1, 0, 0),
                new(new Vector2(hw, -hh), 0, 1, 0),
                new(new Vector2(hw, hh), 0, 0, 1),
                new(new Vector2(-hw, hh), 1, 1, 0)
        };
        var indices = new uint[] { 0, 1, 2, 2, 3, 0 };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex>(device, "QuadVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "QuadIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex>(device, "QuadVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "QuadIBUpload", TransferBufferUsage.Upload, (uint)indices.Length);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        // Dispose upload resources
        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Length);
    }

    /// <summary>
    /// Overload: flat color quad
    /// </summary>
    /// <param name="device"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Mesh CreateQuad(GraphicsDevice device, float width, float height, Vector3 color)
    {
        var hw = width / 2f;
        var hh = height / 2f;
        var verts = new Vertex[]
        {
                new(new Vector2(-hw, -hh), color.X, color.Y, color.Z),
                new(new Vector2(hw, -hh), color.X, color.Y, color.Z),
                new(new Vector2(hw, hh), color.X, color.Y, color.Z),
                new(new Vector2(-hw, hh), color.X, color.Y, color.Z)
        };
        var indices = new uint[] { 0, 1, 2, 2, 3, 0 };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex>(device, "QuadVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "QuadIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex>(device, "QuadVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "QuadIBUpload", TransferBufferUsage.Upload, (uint)indices.Length);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        // Dispose upload resources
        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Length);
    }

    /// <summary>
    /// Creates a new box mesh.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Mesh CreateBox(GraphicsDevice device, float size)
    {
        // For brevity, just a quad for now; full 3D box can be added later
        return CreateQuad(device, size, size);
    }

    /// <summary>
    /// Creates a simple icosphere (approximated sphere) for debug visualization.
    /// </summary>
    public static Mesh CreateSphere3DLit(GraphicsDevice device, float radius, Vector3 color, int subdivisions = 1)
    {
        subdivisions = Math.Max(0, Math.Min(6, subdivisions)); // clamp to keep sizes sane

        // Base icosahedron ------------------------------------------
        var t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

        var unitVerts = new List<Vector3>
        {
            Vector3.Normalize(new Vector3(-1,  t,  0)),
            Vector3.Normalize(new Vector3( 1,  t,  0)),
            Vector3.Normalize(new Vector3(-1, -t,  0)),
            Vector3.Normalize(new Vector3( 1, -t,  0)),
            Vector3.Normalize(new Vector3( 0, -1,  t)),
            Vector3.Normalize(new Vector3( 0,  1,  t)),
            Vector3.Normalize(new Vector3( 0, -1, -t)),
            Vector3.Normalize(new Vector3( 0,  1, -t)),
            Vector3.Normalize(new Vector3( t,  0, -1)),
            Vector3.Normalize(new Vector3( t,  0,  1)),
            Vector3.Normalize(new Vector3(-t,  0, -1)),
            Vector3.Normalize(new Vector3(-t,  0,  1)),
        };

        // 20 faces for an icosahedron
        var faces = new List<(int a, int b, int c)>
        {
            (0,11,5), (0,5,1), (0,1,7), (0,7,10), (0,10,11),
            (1,5,9), (5,11,4), (11,10,2), (10,7,6), (7,1,8),
            (3,9,4), (3,4,2), (3,2,6), (3,6,8), (3,8,9),
            (4,9,5), (2,4,11), (6,2,10), (8,6,7), (9,8,1)
        };

        // Subdivide --------------------------------------------------
        var midpointCache = new Dictionary<(int, int), int>(capacity: faces.Count * 3);

        int GetMidpoint(int i0, int i1)
        {
            var key = i0 < i1 ? (i0, i1) : (i1, i0);
            if (midpointCache.TryGetValue(key, out var cached)) return cached;

            var mid = Vector3.Normalize(unitVerts[i0] + unitVerts[i1]);
            int idx = unitVerts.Count;
            unitVerts.Add(mid);
            midpointCache[key] = idx;
            return idx;
        }

        for (int s = 0; s < subdivisions; s++)
        {
            var newFaces = new List<(int a, int b, int c)>(faces.Count * 4);
            midpointCache.Clear();

            foreach (var (a, b, c) in faces)
            {
                int ab = GetMidpoint(a, b);
                int bc = GetMidpoint(b, c);
                int ca = GetMidpoint(c, a);

                // 4 new faces
                newFaces.Add((a, ab, ca));
                newFaces.Add((b, bc, ab));
                newFaces.Add((c, ca, bc));
                newFaces.Add((ab, bc, ca));
            }

            faces = newFaces;
        }

        // Build lit vertices/index buffer ----------------------------
        var litVerts = new List<Vertex3DMaterial>(unitVerts.Count);
        for (int i = 0; i < unitVerts.Count; i++)
        {
            var n = unitVerts[i];
            var p = n * radius;
            litVerts.Add(new Vertex3DMaterial(p, n, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
        }

        var indices = new List<uint>(faces.Count * 3);
        foreach (var (a, b, c) in faces)
        {
            indices.Add((uint)a);
            indices.Add((uint)b);
            indices.Add((uint)c);
        }

        // Upload to GPU ----------------------------------------------
        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DMaterial>(device, "SphereLitVB", BufferUsageFlags.Vertex, (uint)litVerts.Count);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "SphereLitIB", BufferUsageFlags.Index, (uint)indices.Count);

        var vtransfer = TransferBuffer.Create<Vertex3DMaterial>(device, "SphereLitVBUpload", TransferBufferUsage.Upload, (uint)litVerts.Count);
        var vspan = vtransfer.Map<Vertex3DMaterial>(cycle: false);
        litVerts.ToArray().AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "SphereLitIBUpload", TransferBufferUsage.Upload, (uint)indices.Count);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.ToArray().AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, litVerts.Count, PrimitiveType.TriangleList, ib, indices.Count);
    }

    /// <summary>
    /// Creates a lit plane on the XZ plane centered at origin (Y=0), facing +Y.
    /// </summary>
    public static Mesh CreatePlane3DLit(GraphicsDevice device, float width, float depth, Vector3 color)
    {
        float hw = width / 2f;
        float hd = depth / 2f;
        var up = new Vector3(0, 1, 0);

        // CCW when viewed from +Y (above)
        var verts = new Vertex3DMaterial[]
        {
            new(new Vector3(-hw, 0, -hd), up, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 0
            new(new Vector3(-hw, 0,  hd), up, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 1
            new(new Vector3( hw, 0,  hd), up, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 2
            new(new Vector3( hw, 0, -hd), up, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)), // 3
        };
        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DMaterial>(device, "PlaneLitVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "PlaneLitIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex3DMaterial>(device, "PlaneLitVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex3DMaterial>(cycle: false);
        verts.AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "PlaneLitIBUpload", TransferBufferUsage.Upload, (uint)indices.Length);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Length, PrimitiveType.TriangleList, ib, indices.Length);
    }

    /// <summary>
    /// Creates a lit cylinder aligned on Y axis, centered at origin.
    /// </summary>
    public static Mesh CreateCylinder3DLit(GraphicsDevice device, float radius, float height, Vector3 color, int radialSegments = 24, int heightSegments = 1, bool capped = true)
    {
        radialSegments = Math.Max(3, radialSegments);
        heightSegments = Math.Max(1, heightSegments);
        float halfH = height / 2f;

        var verts = new List<Vertex3DMaterial>();
        var indices = new List<uint>();

        // Body rings (two rings: bottom/top per segment). Smooth shading with radial normals.
        for (int i = 0; i < radialSegments; i++)
        {
            float a = (float)(i * (Math.PI * 2.0) / radialSegments);
            float x = MathF.Cos(a);
            float z = MathF.Sin(a);
            var normal = new Vector3(x, 0, z);
            // bottom and top
            verts.Add(new Vertex3DMaterial(new Vector3(radius * x, -halfH, radius * z), normal, Vector2.Zero, new Vector3(color.X, color.Y, color.Z))); // i*2
            verts.Add(new Vertex3DMaterial(new Vector3(radius * x, halfH, radius * z), normal, Vector2.Zero, new Vector3(color.X, color.Y, color.Z))); // i*2+1
        }

        // Side quads
        for (int i = 0; i < radialSegments; i++)
        {
            int ni = (i + 1) % radialSegments;
            uint b_i = (uint)(i * 2);
            uint t_i = b_i + 1;
            uint b_n = (uint)(ni * 2);
            uint t_n = b_n + 1;
            // CCW from outside
            indices.Add(b_i); indices.Add(b_n); indices.Add(t_n);
            indices.Add(b_i); indices.Add(t_n); indices.Add(t_i);
        }

        // Caps
        if (capped)
        {
            // Top cap
            uint topCenterIndex = (uint)verts.Count;
            verts.Add(new Vertex3DMaterial(new Vector3(0, halfH, 0), new Vector3(0, 1, 0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
            var topStart = (uint)verts.Count;
            for (int i = 0; i < radialSegments; i++)
            {
                float a = (float)(i * (Math.PI * 2.0) / radialSegments);
                float x = MathF.Cos(a);
                float z = MathF.Sin(a);
                verts.Add(new Vertex3DMaterial(new Vector3(radius * x, halfH, radius * z), new Vector3(0, 1, 0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
            }
            for (int i = 0; i < radialSegments; i++)
            {
                uint i0 = (uint)i;
                uint i1 = (uint)((i + 1) % radialSegments);
                indices.Add(topCenterIndex); indices.Add(topStart + i0); indices.Add(topStart + i1);
            }

            // Bottom cap
            uint bottomCenterIndex = (uint)verts.Count;
            verts.Add(new Vertex3DMaterial(new Vector3(0, -halfH, 0), new Vector3(0, -1, 0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
            var bottomStart = (uint)verts.Count;
            for (int i = 0; i < radialSegments; i++)
            {
                float a = (float)(i * (Math.PI * 2.0) / radialSegments);
                float x = MathF.Cos(a);
                float z = MathF.Sin(a);
                verts.Add(new Vertex3DMaterial(new Vector3(radius * x, -halfH, radius * z), new Vector3(0, -1, 0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
            }
            for (int i = 0; i < radialSegments; i++)
            {
                uint i0 = (uint)i;
                uint i1 = (uint)((i + 1) % radialSegments);
                // CCW when viewed from below
                indices.Add(bottomCenterIndex); indices.Add(bottomStart + i1); indices.Add(bottomStart + i0);
            }
        }

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DMaterial>(device, "CylinderLitVB", BufferUsageFlags.Vertex, (uint)verts.Count);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "CylinderLitIB", BufferUsageFlags.Index, (uint)indices.Count);

        var vtransfer = TransferBuffer.Create<Vertex3DMaterial>(device, "CylinderLitVBUpload", TransferBufferUsage.Upload, (uint)verts.Count);
        var vspan = vtransfer.Map<Vertex3DMaterial>(cycle: false);
        verts.ToArray().AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "CylinderLitIBUpload", TransferBufferUsage.Upload, (uint)indices.Count);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.ToArray().AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vtransfer, vb, false);
        copy.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Count, PrimitiveType.TriangleList, ib, indices.Count);
    }

    /// <summary>
    /// Creates a lit capsule aligned on Y axis, centered at origin.
    /// Height is the total tip-to-tip length; radius is the hemisphere radius.
    /// </summary>
    public static Mesh CreateCapsule3DLit(GraphicsDevice device, float radius, float height, Vector3 color, int radialSegments = 24, int hemisphereStacks = 8)
    {
        radialSegments = Math.Max(3, radialSegments);
        hemisphereStacks = Math.Max(1, hemisphereStacks);

        float cylHalf = MathF.Max(0f, (height - 2f * radius) / 2f);

        var verts = new List<Vertex3DMaterial>();
        var indices = new List<uint>();

        // Build bottom hemisphere rings (equator to pole)
        var bottomRings = new List<uint[]>();
        for (int j = 0; j <= hemisphereStacks; j++)
        {
            float phi = (float)j / hemisphereStacks * (MathF.PI / 2f); // 0..pi/2
            float y = -cylHalf - radius * MathF.Sin(phi);
            float ringR = radius * MathF.Cos(phi);
            var ring = new uint[radialSegments];
            for (int i = 0; i < radialSegments; i++)
            {
                float a = (float)(i * (Math.PI * 2.0) / radialSegments);
                float nx = MathF.Cos(a) * MathF.Cos(phi);
                float nz = MathF.Sin(a) * MathF.Cos(phi);
                float ny = -MathF.Sin(phi);
                var normal = new Vector3(nx, ny, nz);
                var pos = new Vector3(ringR * MathF.Cos(a), y, ringR * MathF.Sin(a));
                ring[i] = (uint)verts.Count;
                verts.Add(new Vertex3DMaterial(pos, normal, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
            }
            bottomRings.Add(ring);
        }

        // Connect bottom hemisphere rings
        for (int j = 0; j < bottomRings.Count - 1; j++)
        {
            var aRing = bottomRings[j];
            var bRing = bottomRings[j + 1];
            for (int i = 0; i < radialSegments; i++)
            {
                int ni = (i + 1) % radialSegments;
                // A is higher (closer to equator), B is lower (closer to pole)
                indices.Add(aRing[i]); indices.Add(bRing[ni]); indices.Add(bRing[i]);
                indices.Add(aRing[i]); indices.Add(aRing[ni]); indices.Add(bRing[ni]);
            }
        }

        // Bottom pole (connect last ring to a single pole vertex)
        uint bottomPole = (uint)verts.Count;
        verts.Add(new Vertex3DMaterial(new Vector3(0, -cylHalf - radius, 0), new Vector3(0, -1, 0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
        var lastBottomRing = bottomRings[^1];
        for (int i = 0; i < radialSegments; i++)
        {
            int ni = (i + 1) % radialSegments;
            indices.Add(bottomPole); indices.Add(lastBottomRing[ni]); indices.Add(lastBottomRing[i]);
        }

        // Build top hemisphere rings (equator to pole)
        var topRings = new List<uint[]>();
        for (int j = 0; j <= hemisphereStacks; j++)
        {
            float phi = (float)j / hemisphereStacks * (MathF.PI / 2f); // 0..pi/2
            float y = cylHalf + radius * MathF.Sin(phi);
            float ringR = radius * MathF.Cos(phi);
            var ring = new uint[radialSegments];
            for (int i = 0; i < radialSegments; i++)
            {
                float a = (float)(i * (Math.PI * 2.0) / radialSegments);
                float nx = MathF.Cos(a) * MathF.Cos(phi);
                float nz = MathF.Sin(a) * MathF.Cos(phi);
                float ny = MathF.Sin(phi);
                var normal = new Vector3(nx, ny, nz);
                var pos = new Vector3(ringR * MathF.Cos(a), y, ringR * MathF.Sin(a));
                ring[i] = (uint)verts.Count;
                verts.Add(new Vertex3DMaterial(pos, normal, Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
            }
            topRings.Add(ring);
        }

        // Connect top hemisphere rings
        for (int j = 0; j < topRings.Count - 1; j++)
        {
            var aRing = topRings[j]; // lower (closer to equator)
            var bRing = topRings[j + 1]; // higher (closer to pole)
            for (int i = 0; i < radialSegments; i++)
            {
                int ni = (i + 1) % radialSegments;
                indices.Add(aRing[i]); indices.Add(aRing[ni]); indices.Add(bRing[ni]);
                indices.Add(aRing[i]); indices.Add(bRing[ni]); indices.Add(bRing[i]);
            }
        }

        // Top pole
        uint topPole = (uint)verts.Count;
        verts.Add(new Vertex3DMaterial(new Vector3(0, cylHalf + radius, 0), new Vector3(0, 1, 0), Vector2.Zero, new Vector3(color.X, color.Y, color.Z)));
        var lastTopRing = topRings[^1];
        for (int i = 0; i < radialSegments; i++)
        {
            int ni = (i + 1) % radialSegments;
            indices.Add(topPole); indices.Add(lastTopRing[i]); indices.Add(lastTopRing[ni]);
        }

        // Connect cylinder between bottom equator ring and top equator ring (if any cylinder length)
        if (cylHalf > 0f)
        {
            var bottomEquator = bottomRings[0];
            var topEquator = topRings[0];
            for (int i = 0; i < radialSegments; i++)
            {
                int ni = (i + 1) % radialSegments;
                indices.Add(bottomEquator[i]); indices.Add(bottomEquator[ni]); indices.Add(topEquator[ni]);
                indices.Add(bottomEquator[i]); indices.Add(topEquator[ni]); indices.Add(topEquator[i]);
            }
        }

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DMaterial>(device, "CapsuleLitVB", BufferUsageFlags.Vertex, (uint)verts.Count);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "CapsuleLitIB", BufferUsageFlags.Index, (uint)indices.Count);

        var vtransfer = TransferBuffer.Create<Vertex3DMaterial>(device, "CapsuleLitVBUpload", TransferBufferUsage.Upload, (uint)verts.Count);
        var vspan = vtransfer.Map<Vertex3DMaterial>(cycle: false);
        verts.ToArray().AsSpan().CopyTo(vspan);
        vtransfer.Unmap();

        var itransfer = TransferBuffer.Create<uint>(device, "CapsuleLitIBUpload", TransferBufferUsage.Upload, (uint)indices.Count);
        var ispan = itransfer.Map<uint>(cycle: false);
        indices.ToArray().AsSpan().CopyTo(ispan);
        itransfer.Unmap();

        var cmdbuf = device.AcquireCommandBuffer();
        var copy2 = cmdbuf.BeginCopyPass();
        copy2.UploadToBuffer(vtransfer, vb, false);
        copy2.UploadToBuffer(itransfer, ib, false);
        cmdbuf.EndCopyPass(copy2);
        device.Submit(cmdbuf);

        vtransfer.Dispose();
        itransfer.Dispose();

        return new Mesh(vb, verts.Count, PrimitiveType.TriangleList, ib, indices.Count);
    }

    /// <summary>
    /// Dispose GPU buffers owned by this mesh.
    /// </summary>
    public void Dispose()
    {
        try { VertexBuffer.Dispose(); } catch { }
        try { IndexBuffer?.Dispose(); } catch { }
    }
}
