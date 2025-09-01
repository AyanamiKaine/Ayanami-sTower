using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

// GPU-packed light structs matching HLSL std140-like packing used in the shader.
// GPU-packed light structs matching HLSL std140-like packing used in the shader.
// Use explicit Vector3 + padding floats so individual values (color, intensity, range, etc.)
// are stored in their own fields instead of being mangled into a single Vector4.
// Each logical 16-byte GPU register is represented as either a Vector4 or a Vector3 + float
// padding to guarantee correct alignment.

/// <summary>
/// Light counts uniform (matches HLSL 'LightCounts' cbuffer at b3, space3)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LightCountsUniform
{
    /// <summary>
    /// Number of directional lights in the scene.
    /// </summary>
    public uint directionalLightCount;
    /// <summary>
    /// Number of point lights in the scene.
    /// </summary>
    public uint pointLightCount;
    /// <summary>
    /// Number of spot lights in the scene.
    /// </summary>
    public uint spotLightCount;
    private uint _padding;
}

/// <summary>
/// Represents a directional light source in the scene.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GpuDirectionalLight
{
    /// <summary>
    ///  bytes: direction.xyz + padding
    /// </summary>
    public Vector3 Direction;
    private float _pad0;

    /// <summary>
    ///  bytes: color.xyz + padding
    /// </summary>
    public Vector3 Color;
    /// <summary>
    ///  bytes: intensity (first component) + padding to fill the register
    /// </summary>
    public float Intensity;
}
/// <summary>
/// Represents a point light source in the scene.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPointLight
{
    /// <summary>
    ///  bytes: position.xyz + padding
    /// </summary>
    public Vector3 Position;
    private float _pad0;

    /// <summary>
    ///  bytes: color.xyz + padding
    /// </summary>
    public Vector3 Color;
    /// <summary>
    /// Intensity of the light.
    /// </summary>
    public float Intensity;
    /// <summary>
    /// Effective range of the point light (used for optional smooth cutoff and culling).
    /// </summary>
    public float Range;
    /// <summary>
    /// Attenuation constant term Kc (usually 1.0).
    /// </summary>
    public float Kc;
    /// <summary>
    /// Attenuation linear term Kl.
    /// </summary>
    public float Kl;
    /// <summary>
    /// Attenuation quadratic term Kq.
    /// </summary>
    public float Kq;
}
/// <summary>
/// Represents a spot light source in the scene.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GpuSpotLight
{
    /// <summary>
    ///  bytes: position.xyz + padding
    /// </summary>
    public Vector3 Position;
    private float _pad0;

    /// <summary>
    ///  bytes: direction.xyz + padding
    /// </summary>
    public Vector3 Direction;
    private float _pad1;

    /// <summary>
    ///  bytes: color.xyz + padding
    /// </summary>
    public Vector3 Color;

    /// <summary>
    ///  bytes: range, innerAngle, outerAngle, padding
    /// </summary>
    public float Range;
    /// <summary>
    ///  bytes: innerAngle, outerAngle, padding
    /// </summary>
    public float InnerAngle;
    /// <summary>
    ///  bytes: outerAngle, padding
    /// </summary>
    public float OuterAngle;
    /// <summary>
    /// Attenuation terms: constant (Kc), linear (Kl), quadratic (Kq)
    /// </summary>
    public float Kc;
    /// <summary>
    /// Linear attenuation coefficient Kl.
    /// </summary>
    public float Kl;
    /// <summary>
    /// Quadratic attenuation coefficient Kq.
    /// </summary>
    public float Kq;
    private float _pad2;
    private float _pad3;
}
