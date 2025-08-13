using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.Engine.Rendering;

/// <summary>
/// Pre-pass that renders linear depth into a color cubemap from the point light position.
/// We render to a color cube because SDL GPU lacks a depth-cube sample path in shaders.
/// The resulting texture is sampled in lighting to test shadowing.
/// </summary>
public sealed class ShadowCubeRenderStep : IRenderStep, IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly GraphicsPipeline _pipeline;
    private readonly List<(Func<Mesh> Mesh, Func<Matrix4x4> Model)> _instances = [];

    private readonly uint _size;
    private Texture _cube = null!;

    [StructLayout(LayoutKind.Sequential)]
    private struct VSParams
    {
        public Matrix4x4 Model;
        public Matrix4x4 ViewProj;
        public Vector3 LightPos;
        public float FarPlane;
        public float DepthBias; // bias for receivers, forwarded via renderer separately
        public Vector3 Pad;
    }

    /// <summary>
    /// Settings for the shadow cube rendering.
    /// </summary>
    public struct Settings
    {
        /// <summary>
        /// Near plane used for shadow mapping.
        /// </summary>
        public float NearPlane;
        /// <summary>
        /// Far plane used for shadow mapping.
        /// </summary>
        public float FarPlane;
        /// <summary>
        /// Shadow receiver depth bias to reduce acne.
        /// </summary>
        public float DepthBias;
    }

    private Settings _settings = new() { NearPlane = 0.05f, FarPlane = 25f, DepthBias = 0.01f };

    private Vector3 _lightPos = new(0, 0, 0);
    /// <summary>
    /// Sets the light position for shadow mapping.
    /// </summary>
    public void SetLightPosition(Vector3 position) => _lightPos = position;
    /// <summary>
    /// Creates a new instance of the ShadowCubeRenderStep.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="pipeline"></param>
    /// <param name="size"></param>
    public ShadowCubeRenderStep(GraphicsDevice device, GraphicsPipeline pipeline, uint size = 512)
    {
        _device = device;
        _pipeline = pipeline;
        _size = Math.Max(16u, size);
    }
    /// <summary>
    /// Initializes the shadow cube render step.
    /// </summary>
    /// <param name="device"></param>
    public void Initialize(GraphicsDevice device)
    {
        _cube = Texture.CreateCube(
            device,
            name: "ShadowCube",
            size: _size,
            format: TextureFormat.R8Unorm,
            usageFlags: TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
        );
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try { _cube?.Dispose(); } catch { }
        _instances.Clear();
    }

    /// <inheritdoc/>
    public Texture CubeTexture => _cube;

    /// <inheritdoc/>
    public void SetSettings(float near, float far, float bias)
    {
        _settings.NearPlane = near;
        _settings.FarPlane = far;
        _settings.DepthBias = bias;
    }

    /// <inheritdoc/>
    public void Update(TimeSpan delta) { }
    /// <inheritdoc/>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view)
    {
        for (int face = 0; face < 6; face++)
        {
            var faceTarget = new ColorTargetInfo
            {
                Texture = _cube.Handle,
                MipLevel = 0,
                LayerOrDepthPlane = (uint)face,
                ClearColor = new Color(1f, 1f, 1f, 1f),
                LoadOp = LoadOp.Clear,
                StoreOp = StoreOp.Store
            };

            var subpass = cmdbuf.BeginRenderPass([faceTarget]);
            subpass.BindGraphicsPipeline(_pipeline);
            subpass.SetViewport(new Viewport(_size, _size));

            var vp = FaceViewProj((CubeMapFace)face, _lightPos, _settings.NearPlane, _settings.FarPlane);
            if (_instances.Count > 0)
            {
                foreach (var (meshP, modelP) in _instances)
                {
                    var vs = new VSParams
                    {
                        Model = modelP(),
                        ViewProj = vp,
                        LightPos = _lightPos,
                        FarPlane = _settings.FarPlane,
                        DepthBias = _settings.DepthBias,
                        Pad = Vector3.Zero
                    };
                    cmdbuf.PushVertexUniformData(vs, slot: 0);
                    meshP().Draw(subpass);
                }
            }

            cmdbuf.EndRenderPass(subpass);
        }
    }

    /// <summary>
    /// Adds a mesh as a potential shadow caster.
    /// </summary>
    public void AddCaster(Func<Mesh> meshProvider, Func<Matrix4x4> modelProvider)
    {
        _instances.Add((meshProvider, modelProvider));
    }

    /// <summary>
    /// Clears all casters queued for the shadow pass.
    /// </summary>
    public void ClearCasters() => _instances.Clear();

    /// <inheritdoc />
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view) { }

    private static Matrix4x4 FaceViewProj(CubeMapFace face, Vector3 pos, float near, float far)
    {
        Vector3 target;
        Vector3 up;

        // Standard D3D cubemap orientations (right-handed coordinate system)
        switch (face)
        {
            case CubeMapFace.PositiveX: // +X (right)
                target = pos + Vector3.UnitX;
                up = -Vector3.UnitY;
                break;
            case CubeMapFace.NegativeX: // -X (left)
                target = pos - Vector3.UnitX;
                up = -Vector3.UnitY;
                break;
            case CubeMapFace.PositiveY: // +Y (up)
                target = pos + Vector3.UnitY;
                up = Vector3.UnitZ;
                break;
            case CubeMapFace.NegativeY: // -Y (down)
                target = pos - Vector3.UnitY;
                up = -Vector3.UnitZ;
                break;
            case CubeMapFace.PositiveZ: // +Z (forward)
                target = pos + Vector3.UnitZ;
                up = -Vector3.UnitY;
                break;
            default: // NegativeZ (back)
                target = pos - Vector3.UnitZ;
                up = -Vector3.UnitY;
                break;
        }
        var view = Matrix4x4.CreateLookAt(pos, target, up);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, near, far);
        return view * proj;
    }
}
