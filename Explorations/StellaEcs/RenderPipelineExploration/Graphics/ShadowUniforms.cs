using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace StellaInvicta.Graphics;

/// <summary>
/// Struct for shadow parameters
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ShadowUniforms
{
    /// <summary>
    /// Light view-projection matrix.
    /// </summary>
    public Matrix4x4 LightViewProjection;
    /// <summary>
    /// Bias applied to shadow mapping.
    /// </summary>
    public float ShadowBias;
    /// <summary>
    /// Intensity of the shadow.
    /// </summary>
    public float ShadowIntensity;
    private float _padding1;
    private float _padding2;
}
