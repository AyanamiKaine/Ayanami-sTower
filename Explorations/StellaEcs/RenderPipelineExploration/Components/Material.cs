using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace StellaInvicta.Components;

/// <summary>
/// Represents the material properties of an object.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Material
{
    /// <summary>
    /// The ambient color of the material.
    /// </summary>
    public Vector3 Ambient;
    private float _pad0;
    /// <summary>
    /// The diffuse color of the material.
    /// </summary>
    public Vector3 Diffuse;
    private float _pad1;
    /// <summary>
    /// The specular color of the material.
    /// </summary>
    public Vector3 Specular;
    /// <summary>
    /// The shininess of the material.
    /// </summary>
    public float Shininess;
    /// <summary>
    /// Global multiplier for ambient contribution.
    /// </summary>
    public float AmbientStrength;
    private Vector3 _pad2; // pad to 16-byte boundary (fills the fourth float4)
}
