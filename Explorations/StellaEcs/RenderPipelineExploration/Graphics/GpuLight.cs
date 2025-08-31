using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace StellaInvicta.Graphics;

// GPU-packed light structs matching HLSL std140-like packing used in the shader.
// GPU-packed light structs matching HLSL std140-like packing used in the shader.
// Use explicit Vector3 + padding floats so individual values (color, intensity, range, etc.)
// are stored in their own fields instead of being mangled into a single Vector4.
// Each logical 16-byte GPU register is represented as either a Vector4 or a Vector3 + float
// padding to guarantee correct alignment.

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
    private float _pad1;

    /// <summary>
    ///  bytes: intensity (first component) + padding to fill the register
    /// </summary>
    public float Intensity;
    private Vector3 _pad2;
}
/// <summary>
/// Represents a point light source in the scene.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
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
    private float _pad1;

    /// <summary>
    ///  bytes: range (x) + padding
    /// </summary>
    public float Range;
    private Vector3 _pad2;
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
    private float _pad2;

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
    private float _pad3;
}
