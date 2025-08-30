using System;
using System.Numerics;

namespace StellaInvicta.Components;

/// <summary>
/// Represents the material properties of an object.
/// </summary>
public struct Material
{
    /// <summary>
    /// The ambient color of the material.
    /// </summary>
    public Vector3 Ambient;
    /// <summary>
    /// The diffuse color of the material.
    /// </summary>
    public Vector3 Diffuse;
    /// <summary>
    /// The specular color of the material.
    /// </summary>
    public Vector3 Specular;
    /// <summary>
    /// The shininess of the material.
    /// </summary>
    public float Shininess;
}
