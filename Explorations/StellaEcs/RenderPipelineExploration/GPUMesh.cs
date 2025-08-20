using System;
using MoonWorks.Graphics;
using Buffer = MoonWorks.Graphics.Buffer;

namespace AyanamisTower.StellaEcs.StellaInvicta;

/// <summary>
/// Represents a mesh stored on the GPU.
/// </summary>
public struct GpuMesh : IDisposable
{
    /// <summary>
    /// The vertex buffer containing the mesh's vertex data.
    /// </summary>
    public Buffer VertexBuffer { get; }
    /// <summary>
    /// The index buffer containing the mesh's index data.
    /// </summary>
    public Buffer IndexBuffer { get; }
    /// <summary>
    /// The number of indices in the mesh.
    /// </summary>
    public uint IndexCount { get; }
    /// <summary>
    /// The format of the indices in the mesh.
    /// </summary>
    public IndexElementSize IndexFormat { get; }

    private readonly GraphicsDevice _gd;

    /// <summary>
    /// Creates a new instance of the <see cref="GpuMesh"/> class.
    /// </summary>
    /// <param name="gd"></param>
    /// <param name="vb"></param>
    /// <param name="ib"></param>
    /// <param name="indexCount"></param>
    /// <param name="indexFormat"></param>
    private GpuMesh(GraphicsDevice gd, Buffer vb, Buffer ib, uint indexCount, IndexElementSize indexFormat)
    {
        _gd = gd;
        VertexBuffer = vb;
        IndexBuffer = ib;
        IndexCount = indexCount;
        IndexFormat = indexFormat;
    }
    /// <summary>
    /// Creates a new instance of the <see cref="GpuMesh"/> class.
    /// </summary>
    /// <param name="gd"></param>
    /// <param name="vertices"></param>
    /// <param name="indices"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static GpuMesh Upload(GraphicsDevice gd, ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices, string name)
    {
        var vb = Buffer.Create<Vertex>(gd, $"{name}_VB", BufferUsageFlags.Vertex, (uint)vertices.Length);
        var ib = Buffer.Create<uint>(gd, $"{name}_IB", BufferUsageFlags.Index, (uint)indices.Length);

        var vUpload = TransferBuffer.Create<Vertex>(gd, $"{name}_VB_Upload", TransferBufferUsage.Upload, (uint)vertices.Length);
        var iUpload = TransferBuffer.Create<uint>(gd, $"{name}_IB_Upload", TransferBufferUsage.Upload, (uint)indices.Length);

        var vspan = vUpload.Map<Vertex>(cycle: false);
        vertices.CopyTo(vspan);
        vUpload.Unmap();

        var ispan = iUpload.Map<uint>(cycle: false);
        indices.CopyTo(ispan);
        iUpload.Unmap();

        var cmdbuf = gd.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        copy.UploadToBuffer(vUpload, vb, false);
        copy.UploadToBuffer(iUpload, ib, false);
        cmdbuf.EndCopyPass(copy);
        gd.Submit(cmdbuf);

        vUpload.Dispose();
        iUpload.Dispose();

        return new GpuMesh(gd, vb, ib, (uint)indices.Length, IndexElementSize.ThirtyTwo);
    }
    /// <summary>
    /// Binds the mesh's vertex and index buffers to the specified render pass.
    /// </summary>
    /// <param name="pass"></param>
    public void Bind(RenderPass pass)
    {
        pass.BindVertexBuffers(VertexBuffer);
        pass.BindIndexBuffer(IndexBuffer, IndexFormat);
    }
    /// <summary>
    /// Disposes the resources used by the mesh.
    /// </summary>
    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
    }
}
