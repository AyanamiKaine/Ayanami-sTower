using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using System.Runtime.InteropServices;
using AyanamisTower.StellaEcs.Engine.Rendering.Materials;

namespace AyanamisTower.StellaEcs.Engine.Rendering;

/// <summary>
/// Draws lit textured meshes (requires Mesh.Vertex3DTexturedLit) using a single bound texture/sampler.
/// </summary>
public sealed class TexturedLitMeshInstancesRenderStep(GraphicsPipeline pipeline, Texture texture, Sampler sampler) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly Texture _texture = texture;
    private readonly Sampler _sampler = sampler;
    private readonly List<(Func<Mesh> Mesh, Func<Matrix4x4> Model)> _instances = [];
    private readonly LitMaterialCommon _common = new();

    /// <inheritdoc/>
    public void SetLight(Vector3 position, Vector3 color, float ambient = 0.2f) => _common.SetLight(position, color, ambient);

    /// <summary>
    /// Sets shadow parameters for point light shadow mapping.
    /// </summary>
    public void SetShadowParams(float farPlane, float depthBias) => _common.SetShadowParams(farPlane, depthBias);

    /// <summary>
    /// Sets the shadow cubemap and its sampler used in the textured-lit shader.
    /// </summary>
    public void SetShadowMap(Texture cube, Sampler sampler) => _common.SetShadowMap(cube, sampler);

    /// <inheritdoc/>
    public void Initialize(GraphicsDevice device) { }
    /// <inheritdoc/>
    public void Update(TimeSpan delta) { }
    /// <inheritdoc/>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view) { }

    /// <inheritdoc/>
    public void AddInstance(Func<Mesh> meshProvider, Func<Matrix4x4> modelProvider)
    {
        _instances.Add((meshProvider, modelProvider));
    }

    /// <inheritdoc/>
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        if (_instances.Count == 0) return;
        pass.BindGraphicsPipeline(_pipeline);
        if (_common.TryGetShadowBinding(out var shadowBinding))
        {
            Span<TextureSamplerBinding> bindings = stackalloc TextureSamplerBinding[2];
            bindings[0] = new TextureSamplerBinding(_texture, _sampler); // t0/s0 - albedo
            bindings[1] = shadowBinding;                                 // t1/s1 - shadow cube
            pass.BindFragmentSamplers(bindings);
        }
        else
        {
            pass.BindFragmentSamplers(new TextureSamplerBinding(_texture, _sampler));
        }

        foreach (var (meshP, modelP) in _instances)
        {
            var m = modelP();
            var mvp = m * view.View * view.Projection;
            var vsParams = _common.BuildVSParams(mvp, m);
            cmdbuf.PushVertexUniformData(vsParams, slot: 0);
            meshP().Draw(pass);
        }
    }

    /// <summary>
    /// Clears all queued instances for this step.
    /// </summary>
    public void ClearInstances() => _instances.Clear();

    /// <inheritdoc/>
    public void Dispose() => _instances.Clear();
}
