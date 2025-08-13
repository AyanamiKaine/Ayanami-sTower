using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.Engine.Rendering;

/// <summary>
/// Draws lit meshes, pushing model and MVP as vertex uniforms and a simple point light as fragment uniforms.
/// </summary>
public sealed class LitMeshInstancesRenderStep(GraphicsPipeline pipeline) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly List<(Func<Mesh> Mesh, Func<Matrix4x4> Model)> _instances = [];

    [StructLayout(LayoutKind.Sequential)]
    private struct VSParams
    {
        public Matrix4x4 MVP;
        public Matrix4x4 Model;
        public Vector3 LightPos;
        public float Ambient;
        public Vector3 LightColor;
        public float FarPlane;
        public float DepthBias;
    }

    /// <summary>
    /// Light parameters for the lit 3D pipeline.
    /// </summary>
    public struct LightParams
    {
        /// <summary>
        /// Position of the light in 3D space.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Ambient light intensity.
        /// </summary>
        public float Ambient;
        /// <summary>
        /// Diffuse light color.
        /// </summary>
        public Vector3 Color;
        /// <summary>
        /// Far plane used to normalize shadow map depth.
        /// </summary>
        public float FarPlane;
        /// <summary>
        /// Shadow receiver depth bias to reduce acne.
        /// </summary>
        public float DepthBias;
    }

    private LightParams _light = new()
    {
        Position = new Vector3(2, 3, 2),
        Ambient = 0.2f,
        Color = new Vector3(1, 1, 1),
        FarPlane = 25f,
        DepthBias = 0.01f
    };
    /// <summary>
    /// Sets the point light (sun-like) parameters used by the lit 3D pipeline.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="ambient"></param>
    public void SetLight(Vector3 position, Vector3 color, float ambient = 0.2f)
    {
        _light.Position = position;
        _light.Color = color;
        _light.Ambient = ambient;
    }

    /// <summary>
    /// Sets shadow parameters for the point light shadow mapping.
    /// </summary>
    public void SetShadowParams(float farPlane, float depthBias)
    {
        _light.FarPlane = farPlane;
        _light.DepthBias = depthBias;
    }

    private Texture? _shadowCube;
    private Sampler? _shadowSampler;
    /// <summary>
    /// Sets the shadow cubemap texture and its sampler.
    /// </summary>
    public void SetShadowMap(Texture shadowCube, Sampler sampler)
    {
        _shadowCube = shadowCube;
        _shadowSampler = sampler;
    }

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
        if (_shadowCube != null && _shadowSampler != null)
        {
            // Bind shadow cube/sampler at t0/s0 in fragment space (space2 in shader)
            Span<TextureSamplerBinding> sb = stackalloc TextureSamplerBinding[1];
            sb[0] = new TextureSamplerBinding(_shadowCube, _shadowSampler);
            pass.BindFragmentSamplers(sb);
        }
        foreach (var (meshP, modelP) in _instances)
        {
            var m = modelP();
            var mvp = m * view.View * view.Projection;
            // Push combined VS params (mvp, model, light) to b0 (space1)
            var vsParams = new VSParams
            {
                MVP = mvp,
                Model = m,
                LightPos = _light.Position,
                Ambient = _light.Ambient,
                LightColor = _light.Color,
                FarPlane = _light.FarPlane,
                DepthBias = _light.DepthBias
            };
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
