using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using System.Runtime.InteropServices;

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
    /// Light parameters.
    /// </summary>
    public struct Light
    {
        /// <summary>
        /// Light position in world space.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Light ambient intensity.
        /// </summary>
        public float Ambient;
        /// <summary>
        /// Light color.
        /// </summary>
        public Vector3 Color;
        /// <summary>
        /// Padding to align struct size.
        /// </summary>
        public float FarPlane;
        /// <summary>
        /// Shadow receiver depth bias to reduce acne.
        /// </summary>
        public float DepthBias;
    }

    private Light _light = new()
    {
        Position = new Vector3(2, 3, 2),
        Ambient = 0.2f,
        Color = new Vector3(1, 1, 1),
        FarPlane = 25f,
        DepthBias = 0.01f
    };

    /// <inheritdoc/>
    public void SetLight(Vector3 position, Vector3 color, float ambient = 0.2f)
    {
        _light.Position = position;
        _light.Color = color;
        _light.Ambient = ambient;
    }

    /// <summary>
    /// Sets shadow parameters for point light shadow mapping.
    /// </summary>
    public void SetShadowParams(float farPlane, float depthBias)
    {
        _light.FarPlane = farPlane;
        _light.DepthBias = depthBias;
    }

    private Texture? _shadowCube;
    private Sampler? _shadowSampler;
    /// <summary>
    /// Sets the shadow cubemap and its sampler used in the textured-lit shader.
    /// </summary>
    public void SetShadowMap(Texture cube, Sampler sampler)
    {
        _shadowCube = cube;
        _shadowSampler = sampler;
    }

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
        pass.BindFragmentSamplers(new TextureSamplerBinding(_texture, _sampler));
        if (_shadowCube != null && _shadowSampler != null)
        {
            Span<TextureSamplerBinding> sb = stackalloc TextureSamplerBinding[1];
            sb[0] = new TextureSamplerBinding(_shadowCube, _shadowSampler);
            pass.BindFragmentSamplers(1, sb);
        }
        foreach (var (meshP, modelP) in _instances)
        {
            var m = modelP();
            var mvp = m * view.View * view.Projection;
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

    /// <summary>
    /// Clears all queued instances for this step.
    /// </summary>
    public void ClearInstances() => _instances.Clear();

    /// <inheritdoc/>
    public void Dispose() => _instances.Clear();
}
