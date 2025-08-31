using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// Struct for vertex uniforms matching the HLSL cbuffer
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexUniforms
{
    /// <summary>
    /// Model-view-projection matrix.
    /// </summary>
    public Matrix4x4 MVP;
    /// <summary>
    /// Model matrix.
    /// </summary>
    public Matrix4x4 Model;
    /// <summary>
    /// World-space model matrix (no camera-relative subtraction). Used for shadow sampling.
    /// </summary>
    public Matrix4x4 ModelWorld;
    /// <summary>
    /// World-space camera position. Used by shaders to compute view direction in world space.
    /// </summary>
    public Vector3 CameraPosition;
    // Padding to align to 16 bytes if needed
    private float _pad0;
}

