using System;

namespace StellaInvicta.Components;

// Per-entity impostor override component.
// Attach this to an entity to customize how its tiny 2D impostor is drawn when far away.
// All overrides are optional; when an Override* flag is false, the global setting from the debug UI is used.
/// <summary>
/// Per-entity impostor overrides for the tiny 2D icon drawn when the object is very small on screen.
/// Attach to an entity to customize color, shape, size range, visibility threshold, and border thickness.
/// </summary>
public struct Impostor
{
    /// <summary>
    /// When false, suppress drawing an impostor for this entity (in addition to the global toggle).
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// If true, use <see cref="Color"/> instead of the global impostor color.
    /// </summary>
    public bool OverrideColor;
    /// <summary>
    /// Per-entity impostor color (ImGui RGBA, 0..1 per channel).
    /// </summary>
    public System.Numerics.Vector4 Color;

    /// <summary>
    /// If true, use <see cref="ShapeIndex"/> instead of the global shape. 0=Circle, 1=Square.
    /// </summary>
    public bool OverrideShape;
    /// <summary>
    /// Shape index override. 0 = Circle, 1 = Square.
    /// </summary>
    public int ShapeIndex;

    /// <summary>
    /// If true, use <see cref="MinPixelRadius"/> and <see cref="MaxPixelRadius"/> instead of global min/max icon radii.
    /// </summary>
    public bool OverrideMinMax;
    /// <summary>
    /// Minimum icon radius in pixels when the object is extremely tiny on screen.
    /// </summary>
    public float MinPixelRadius;
    /// <summary>
    /// Maximum icon radius in pixels when the object is just below the show threshold.
    /// </summary>
    public float MaxPixelRadius;

    /// <summary>
    /// If true, use <see cref="BorderThickness"/> instead of the global border thickness.
    /// </summary>
    public bool OverrideBorder;
    /// <summary>
    /// Impostor border thickness in pixels.
    /// </summary>
    public float BorderThickness;

    /// <summary>
    /// If true, use <see cref="ShowBelowPx"/> instead of the global show-below threshold.
    /// </summary>
    public bool OverrideShowBelow;
    /// <summary>
    /// Draw an impostor when the object's projected radius is below this many pixels.
    /// </summary>
    public float ShowBelowPx;

    /// <summary>
    /// If true, use <see cref="MaxDistance"/> instead of the global max impostor distance.
    /// When the camera is farther than this distance from the entity, the impostor will not be drawn.
    /// </summary>
    public bool OverrideMaxDistance;
    /// <summary>
    /// Maximum camera distance (in world units) at which the impostor can be shown. 0 = unlimited.
    /// </summary>
    public float MaxDistance;

    /// <summary>
    /// If true, use <see cref="ScaleFactor"/> to multiply the computed impostor icon radius.
    /// This lets an entity's impostor appear larger or smaller than the size-based default.
    /// </summary>
    public bool OverrideScaleFactor;
    /// <summary>
    /// Scale multiplier for the computed icon radius. 1 = unchanged, 2 = twice as large, 0.5 = half size.
    /// </summary>
    public float ScaleFactor;

    /// <summary>
    /// Creates a new Impostor override with sensible defaults. Enabled defaults to true so merely adding
    /// this component does not disable impostors unless explicitly set.
    /// </summary>
    public Impostor()
    {
        Enabled = true;
        OverrideColor = false; Color = default;
        OverrideShape = false; ShapeIndex = 0;
        OverrideMinMax = false; MinPixelRadius = 0f; MaxPixelRadius = 0f;
        OverrideBorder = false; BorderThickness = 0f;
        OverrideShowBelow = false; ShowBelowPx = 0f;
        OverrideMaxDistance = false; MaxDistance = 0f;
    OverrideScaleFactor = false; ScaleFactor = 1f;
    }
}
