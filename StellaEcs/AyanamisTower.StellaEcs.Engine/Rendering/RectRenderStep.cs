using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Rendering;

/// <summary>
/// Renders a colored quad in orthographic space using provided pipeline and mesh.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RectRenderStep"/> class.
/// </remarks>
/// <param name="pipeline"></param>
/// <param name="meshProvider"></param>
/// <param name="mvpProvider"></param>
public sealed class RectRenderStep(GraphicsPipeline pipeline, Func<Mesh> meshProvider, Func<Matrix4x4> mvpProvider) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly Func<Mesh> _meshProvider = meshProvider;
    private readonly Func<Matrix4x4> _mvpProvider = mvpProvider;

    /// <summary>
    /// Initializes the rectangle render step.
    /// </summary>
    /// <param name="device"></param>
    public void Initialize(GraphicsDevice device) { }
    /// <summary>
    /// Updates the rectangle render step.
    /// </summary>
    /// <param name="delta"></param>
    public void Update(TimeSpan delta) { }
    /// <summary>
    /// Prepares the rectangle for rendering.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="view"></param>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view) { }
    /// <summary>
    /// Records the rectangle rendering commands.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="pass"></param>
    /// <param name="view"></param>
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        pass.BindGraphicsPipeline(_pipeline);
        cmdbuf.PushVertexUniformData(_mvpProvider());
        _meshProvider().Draw(pass);
    }

    /// <inheritdoc/>
    public void Dispose() { }
}

