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
    /// Creates a flat-colored cube mesh centered at origin with given size and color.
    /// </summary>
    public static Mesh CreateBox3D(GraphicsDevice device, float size, Vector3 color)
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
    /// Creates a cube mesh with per-face normals for lighting and flat albedo color.
    /// </summary>
    public static Mesh CreateBox3DLit(GraphicsDevice device, float size, Vector3 color)
    {
        float h = size / 2f;
        // 24 vertices: 4 per face with face normals
        // Define faces: each with normal and 4 corners CCW
        var verts = new Vertex3DLit[]
        {
            // -Z (back)
            new(new Vector3(-h,-h,-h), new Vector3(0,0,-1), color.X, color.Y, color.Z),
            new(new Vector3( h,-h,-h), new Vector3(0,0,-1), color.X, color.Y, color.Z),
            new(new Vector3( h, h,-h), new Vector3(0,0,-1), color.X, color.Y, color.Z),
            new(new Vector3(-h, h,-h), new Vector3(0,0,-1), color.X, color.Y, color.Z),
            // +Z (front)
            new(new Vector3(-h,-h, h), new Vector3(0,0, 1), color.X, color.Y, color.Z),
            new(new Vector3( h,-h, h), new Vector3(0,0, 1), color.X, color.Y, color.Z),
            new(new Vector3( h, h, h), new Vector3(0,0, 1), color.X, color.Y, color.Z),
            new(new Vector3(-h, h, h), new Vector3(0,0, 1), color.X, color.Y, color.Z),
            // -Y (bottom)
            new(new Vector3(-h,-h,-h), new Vector3(0,-1,0), color.X, color.Y, color.Z),
            new(new Vector3( h,-h,-h), new Vector3(0,-1,0), color.X, color.Y, color.Z),
            new(new Vector3( h,-h, h), new Vector3(0,-1,0), color.X, color.Y, color.Z),
            new(new Vector3(-h,-h, h), new Vector3(0,-1,0), color.X, color.Y, color.Z),
            // +Y (top)
            new(new Vector3(-h, h,-h), new Vector3(0, 1,0), color.X, color.Y, color.Z),
            new(new Vector3( h, h,-h), new Vector3(0, 1,0), color.X, color.Y, color.Z),
            new(new Vector3( h, h, h), new Vector3(0, 1,0), color.X, color.Y, color.Z),
            new(new Vector3(-h, h, h), new Vector3(0, 1,0), color.X, color.Y, color.Z),
            // -X (left)
            new(new Vector3(-h,-h,-h), new Vector3(-1,0,0), color.X, color.Y, color.Z),
            new(new Vector3(-h, h,-h), new Vector3(-1,0,0), color.X, color.Y, color.Z),
            new(new Vector3(-h, h, h), new Vector3(-1,0,0), color.X, color.Y, color.Z),
            new(new Vector3(-h,-h, h), new Vector3(-1,0,0), color.X, color.Y, color.Z),
            // +X (right)
            new(new Vector3( h,-h,-h), new Vector3( 1,0,0), color.X, color.Y, color.Z),
            new(new Vector3( h, h,-h), new Vector3( 1,0,0), color.X, color.Y, color.Z),
            new(new Vector3( h, h, h), new Vector3( 1,0,0), color.X, color.Y, color.Z),
            new(new Vector3( h,-h, h), new Vector3( 1,0,0), color.X, color.Y, color.Z),
        };
        var indices = new uint[]
        {
            // -Z (back): looking from front, CCW is 0,1,2 then 0,2,3  
            0,1,2, 0,2,3,       
            // +Z (front): looking from back, CCW is 4,6,5 then 4,7,6
            4,6,5, 4,7,6,       
            // -Y (bottom): looking from top, CCW is 8,9,10 then 8,10,11
            8,9,10, 8,10,11,    
            // +Y (top): looking from bottom, CCW is 12,14,13 then 12,15,14
            12,14,13, 12,15,14, 
            // -X (left): looking from right, CCW is 16,18,17 then 16,19,18
            16,18,17, 16,19,18, 
            // +X (right): looking from left, CCW is 20,21,22 then 20,22,23
            20,21,22, 20,22,23
        };

        var vb = MoonWorks.Graphics.Buffer.Create<Vertex3DLit>(device, "BoxLitVB", BufferUsageFlags.Vertex, (uint)verts.Length);
        var ib = MoonWorks.Graphics.Buffer.Create<uint>(device, "BoxLitIB", BufferUsageFlags.Index, (uint)indices.Length);

        var vtransfer = TransferBuffer.Create<Vertex3DLit>(device, "BoxLitVBUpload", TransferBufferUsage.Upload, (uint)verts.Length);
        var vspan = vtransfer.Map<Vertex3DLit>(cycle: false);
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
    /// Dispose GPU buffers owned by this mesh.
    /// </summary>
    public void Dispose()
    {
        try { VertexBuffer.Dispose(); } catch { }
        try { IndexBuffer?.Dispose(); } catch { }
    }
}
