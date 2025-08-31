using System;
using System.Runtime.InteropServices;
using StellaInvicta.Components;

namespace StellaInvicta.Graphics;

/// <summary>
/// Material properties uniform (matches HLSL 'MaterialProperties' cbuffer at b4, space3)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MaterialPropertiesUniform
{
    /// <summary>
    /// The material properties.
    /// </summary>
    public Material material;
}
