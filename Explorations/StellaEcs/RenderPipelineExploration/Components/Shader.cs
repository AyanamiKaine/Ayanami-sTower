using System;
using StellaInvicta.Graphics;

namespace StellaInvicta.Components;


/// <summary>
/// Represents a shader configuration for an entity.
/// This component allows entities to specify which shaders to use for rendering.
/// </summary>
public class Shader
{
    /// <summary>
    /// Gets or sets the rendering mode for the shader.
    /// </summary>
    public RenderMode RenderMode { get; set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class.
    /// </summary>
    public Shader()
    {
        RenderMode = new RenderMode();
    }
}
