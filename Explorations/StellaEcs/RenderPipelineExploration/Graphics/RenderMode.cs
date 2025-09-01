using System;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;


/// <summary>
/// Represents the rendering mode for a shader, composed of various sub-modes.
/// </summary>
public class RenderMode
{
    /// <summary>
    /// Gets or sets the blending mode.
    /// </summary>
    public BlendMode Blend { get; set; }

    /// <summary>
    /// Gets or sets the culling mode.
    /// </summary>
    public CullMode Cull { get; set; }

    /// <summary>
    /// Gets or sets the depth mode.
    /// </summary>
    public DepthMode Depth { get; set; }

    /// <summary>
    /// Gets or sets the wireframe mode.
    /// </summary>
    public WireframeMode Wireframe { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderMode"/> class with default values.
    /// </summary>
    public RenderMode()
    {
        Blend = BlendMode.NoBlend;
        Cull = CullMode.CounterClockwiseBack;
        Depth = DepthMode.PrepassAlpha;
        Wireframe = WireframeMode.None;
    }
}
