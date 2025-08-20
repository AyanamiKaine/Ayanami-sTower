using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta;

/// <summary>
/// Simple, efficient batched 3D line renderer.
/// Call Begin() each frame, AddLine() N times, then UploadBufferData() before the render pass, and finally Render().
/// </summary>
internal sealed class LineBatch3D : GraphicsResource
{
    private const int DefaultMaxLines = 8192;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LineVertex : IVertexType
    {
        public Vector3 Position;
        public Vector4 Color;

        public LineVertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }

        public static VertexElementFormat[] Formats =>
            [VertexElementFormat.Float3, VertexElementFormat.Float4];

        public static uint[] Offsets => [0u, 12u];
    }

    public global::MoonWorks.Graphics.Buffer VertexBuffer { get; }
    private readonly global::MoonWorks.Graphics.TransferBuffer _transferBuffer;
    private LineVertex[] _staging;
    private bool _isBegun;
    private readonly int _maxVertices;

    public uint VertexCount { get; private set; }

    public LineBatch3D(GraphicsDevice device, int maxLines = DefaultMaxLines) : base(device)
    {
        var maxVerts = Math.Max(1, maxLines) * 2; // 2 vertices per line
        _maxVertices = maxVerts;
        VertexBuffer = global::MoonWorks.Graphics.Buffer.Create<LineVertex>(device, "LineBatch3D VertexBuffer", BufferUsageFlags.Vertex, (uint)maxVerts);
        _transferBuffer = global::MoonWorks.Graphics.TransferBuffer.Create<byte>(device, "LineBatch3D TransferBuffer", TransferBufferUsage.Upload, VertexBuffer.Size);
        _staging = new LineVertex[maxVerts];
        Name = "LineBatch3D";
    }

    /// <summary>
    /// Must be called once per frame before adding lines.
    /// </summary>
    public void Begin()
    {
        _isBegun = true;
        VertexCount = 0;
    }

    /// <summary>
    /// Adds a line. Returns false if the batch is full.
    /// </summary>
    public bool AddLine(Vector3 a, Vector3 b, Color color)
    {
        return AddLine(a, b, color.ToVector4());
    }

    /// <summary>
    /// Adds a line. Returns false if the batch is full.
    /// </summary>
    public bool AddLine(Vector3 a, Vector3 b, Vector4 color)
    {
        if (!_isBegun)
        {
            throw new InvalidOperationException("Call Begin() before AddLine().");
        }
        if (VertexCount + 2 > _maxVertices)
        {
            return false; // batch full, user can choose to flush and continue if desired
        }

        _staging[(int)VertexCount + 0] = new LineVertex(a, color);
        _staging[(int)VertexCount + 1] = new LineVertex(b, color);
        VertexCount += 2;
        return true;
    }

    /// <summary>
    /// Upload the accumulated vertex data to the GPU. Call this before starting your RenderPass.
    /// </summary>
    public void UploadBufferData(CommandBuffer commandBuffer)
    {
        if (!_isBegun)
        {
            return;
        }
        _isBegun = false;

        if (VertexCount == 0)
        {
            return;
        }
        // Map and copy staging data to transfer buffer
        var span = _transferBuffer.Map<LineVertex>(true);
        var count = (int)VertexCount;
        _staging.AsSpan(0, count).CopyTo(span);
        _transferBuffer.Unmap();

        var copyPass = commandBuffer.BeginCopyPass();
        copyPass.UploadToBuffer(
            new TransferBufferLocation { TransferBuffer = _transferBuffer.Handle, Offset = 0 },
            new BufferRegion { Buffer = VertexBuffer.Handle, Offset = 0, Size = (uint)(count * Marshal.SizeOf<LineVertex>()) },
            true
        );
        commandBuffer.EndCopyPass(copyPass);
    }

    /// <summary>
    /// Binds the vertex buffer and issues one draw call as a line list.
    /// The active GraphicsPipeline must use PrimitiveType.LineList and expect LineVertex inputs.
    /// </summary>
    public void Render(RenderPass pass, Matrix4x4 viewProjection)
    {
        if (VertexCount == 0)
        {
            return;
        }

        // Push VP matrix as vertex uniform at slot 0 (cbuffer b0, space1)
        pass.CommandBuffer.PushVertexUniformData(viewProjection, slot: 0);
        pass.BindVertexBuffers(VertexBuffer);
        pass.DrawPrimitives(VertexCount, 1, 0, 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                VertexBuffer.Dispose();
                _transferBuffer.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}
