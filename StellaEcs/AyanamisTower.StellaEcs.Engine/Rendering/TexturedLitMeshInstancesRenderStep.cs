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
        public float Pad;
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
        public float _pad;
    }

    private Light _light = new()
    {
        Position = new Vector3(2, 3, 2),
        Ambient = 0.2f,
        Color = new Vector3(1, 1, 1)
    };

    /// <inheritdoc/>
    public void SetLight(Vector3 position, Vector3 color, float ambient = 0.2f)
    {
        _light.Position = position;
        _light.Color = color;
        _light.Ambient = ambient;
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
                Pad = 0f
            };
            cmdbuf.PushVertexUniformData(vsParams, slot: 0);
            meshP().Draw(pass);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _instances.Clear();
}
