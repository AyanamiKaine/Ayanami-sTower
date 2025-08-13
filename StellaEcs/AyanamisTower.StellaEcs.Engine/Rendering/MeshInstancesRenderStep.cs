using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Rendering;

/// <summary>
/// Simple step that draws a list of meshes with per-instance model matrices using a single pipeline.
/// </summary>
public sealed class MeshInstancesRenderStep(GraphicsPipeline pipeline, MultiplyMode mode = MultiplyMode.WorldViewProj) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly MultiplyMode _mode = mode;

    private readonly List<(Func<Mesh> mesh, Func<Matrix4x4> model)> _instances = [];

    /// <summary>
    /// Initializes GPU resources (none for this simple step).
    /// </summary>
    /// <param name="device">The graphics device.</param>
    public void Initialize(GraphicsDevice device) { }
    /// <summary>
    /// Updates per-frame CPU-side state (none here).
    /// </summary>
    /// <param name="delta">Delta time.</param>
    public void Update(TimeSpan delta) { }
    /// <summary>
    /// Prepares uploads or pre-pass work (none in this step).
    /// </summary>
    /// <param name="cmdbuf">Command buffer.</param>
    /// <param name="view">View context.</param>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view) { }

    /// <summary>
    /// Adds a mesh instance with a dynamic model matrix provider.
    /// </summary>
    public void AddInstance(Func<Mesh> meshProvider, Func<Matrix4x4> modelProvider)
    {
        _instances.Add((meshProvider, modelProvider));
    }

    /// <summary>
    /// Records draw calls for each instance using the shared pipeline.
    /// </summary>
    /// <param name="cmdbuf">Command buffer.</param>
    /// <param name="pass">Render pass.</param>
    /// <param name="view">View context.</param>
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        if (_instances.Count == 0) { return; }
        pass.BindGraphicsPipeline(_pipeline);
        foreach (var (mesh, model) in _instances)
        {
            var m = model();
            Matrix4x4 mvp = _mode == MultiplyMode.WorldViewProj
                ? m * view.View * view.Projection
                : m * view.OrthoPixels;
            cmdbuf.PushVertexUniformData(mvp);
            mesh().Draw(pass);
        }
    }

    /// <summary>
    /// Disposes per-step state.
    /// </summary>
    public void Dispose()
    {
        _instances.Clear();
    }
}

/// <summary>
/// How to compose the model matrix with the camera matrices for this step.
/// </summary>
public enum MultiplyMode
{
    /// <summary>
    /// Use model * view * projection (3D world space rendering).
    /// </summary>
    WorldViewProj,
    /// <summary>
    /// Use model * orthoPixels (2D pixel-space rendering).
    /// </summary>
    OrthoPixels
}
