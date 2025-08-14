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
/// Draws lit meshes, pushing model and MVP as vertex uniforms and a simple point light as fragment uniforms.
/// </summary>
public sealed class LitMeshInstancesRenderStep(GraphicsPipeline pipeline) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly List<(Func<Mesh> Mesh, Func<Matrix4x4> Model)> _instances = [];
    private readonly LitMaterialCommon _common = new();
    /// <summary>
    /// Sets the point light (sun-like) parameters used by the lit 3D pipeline.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="ambient"></param>
    public void SetLight(Vector3 position, Vector3 color, float ambient = 0.2f) => _common.SetLight(position, color, ambient);

    /// <summary>
    /// Sets shadow parameters for the point light shadow mapping.
    /// </summary>
    public void SetShadowParams(float farPlane, float depthBias) => _common.SetShadowParams(farPlane, depthBias);

    /// <summary>
    /// Sets the shadow cubemap texture and its sampler.
    /// </summary>
    public void SetShadowMap(Texture shadowCube, Sampler sampler) => _common.SetShadowMap(shadowCube, sampler);

    /// <inheritdoc/>
    public void Initialize(GraphicsDevice device) { }

    /// <inheritdoc/>
    public void Update(TimeSpan delta) { }
    /// <inheritdoc/>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view) { }
    /// <summary>
    /// Adds a lit mesh instance (requires Vertex3DLit layout) using the point light.
    /// </summary>
    /// <param name="meshProvider"></param>
    /// <param name="modelProvider"></param>
    public void AddInstance(Func<Mesh> meshProvider, Func<Matrix4x4> modelProvider)
    {
        _instances.Add((meshProvider, modelProvider));
    }
    /// <summary>
    /// Records the draw calls for the lit mesh instances.
    /// </summary>
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        if (_instances.Count == 0) return;
        pass.BindGraphicsPipeline(_pipeline);
        if (_common.TryGetShadowBinding(out var shadowBinding))
        {
            pass.BindFragmentSamplers(shadowBinding);
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

    /// <inheritdoc/>
    public void Dispose() => _instances.Clear();

    /// <summary>
    /// Clears all queued instances. Useful for per-frame submission patterns.
    /// </summary>
    public void ClearInstances() => _instances.Clear();
}
