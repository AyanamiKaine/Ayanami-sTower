using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Rendering;


/// <summary>
/// Renders the spinning cube using provided pipeline and mesh provider.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="CubeRenderStep"/> class.
/// </remarks>
/// <param name="pipeline"></param>
/// <param name="meshProvider"></param>
/// <param name="modelProvider"></param>
public sealed class CubeRenderStep(GraphicsPipeline pipeline, Func<Mesh> meshProvider, Func<Matrix4x4> modelProvider) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly Func<Mesh> _meshProvider = meshProvider;
    private readonly Func<Matrix4x4> _modelProvider = modelProvider;

    /// <summary>
    /// Initializes the cube render step.
    /// </summary>
    /// <param name="device"></param>
    public void Initialize(GraphicsDevice device) { }
    /// <summary>
    /// Updates the cube render step.
    /// </summary>
    /// <param name="delta"></param>
    public void Update(TimeSpan delta) { }
    /// <summary>
    /// Prepares the cube render step for the current frame.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="view"></param>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view) { }
    /// <summary>
    /// Records the cube render step.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="pass"></param>
    /// <param name="view"></param>

    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        pass.BindGraphicsPipeline(_pipeline);
        cmdbuf.PushVertexUniformData(_modelProvider() * view.View * view.Projection);
        _meshProvider().Draw(pass);
    }
    /// <summary>
    /// Disposes the cube render step.
    /// </summary>
    public void Dispose() { }
}

