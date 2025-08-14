using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Rendering.Materials;

/// <summary>
/// Shared light/shadow state and uniform packing for point-lit passes.
/// Steps can use this to avoid duplicating light structs and shadow bindings.
/// </summary>
internal sealed class LitMaterialCommon
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VSParams
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
    /// Public light parameters for convenience when exposing through higher layers.
    /// </summary>
    public struct LightParams
    {
        public Vector3 Position;
        public float Ambient;
        public Vector3 Color;
        public float FarPlane;
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

    private Texture? _shadowCube;
    private Sampler? _shadowSampler;

    public void SetLight(Vector3 position, Vector3 color, float ambient = 0.2f)
    {
        _light.Position = position;
        _light.Color = color;
        _light.Ambient = ambient;
    }

    public void SetShadowParams(float farPlane, float depthBias)
    {
        _light.FarPlane = farPlane;
        _light.DepthBias = depthBias;
    }

    public void SetShadowMap(Texture shadowCube, Sampler sampler)
    {
        _shadowCube = shadowCube;
        _shadowSampler = sampler;
    }

    /// <summary>
    /// If a shadow map is configured, returns a binding that callers can merge with their own samplers.
    /// </summary>
    public bool TryGetShadowBinding(out TextureSamplerBinding binding)
    {
        if (_shadowCube != null && _shadowSampler != null)
        {
            binding = new TextureSamplerBinding(_shadowCube, _shadowSampler);
            return true;
        }
        binding = default;
        return false;
    }

    /// <summary>
    /// Packs the vertex uniform parameters with the current light state.
    /// </summary>
    public VSParams BuildVSParams(Matrix4x4 mvp, Matrix4x4 model)
    {
        return new VSParams
        {
            MVP = mvp,
            Model = model,
            LightPos = _light.Position,
            Ambient = _light.Ambient,
            LightColor = _light.Color,
            FarPlane = _light.FarPlane,
            DepthBias = _light.DepthBias
        };
    }
}
