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
