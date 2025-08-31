namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// Represents blending modes for rendering.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Premultiplied alpha blending (colors are already premultiplied by alpha).
    /// </summary>
    PremultipliedAlpha,
    /// <summary>
    /// Additive blending (adds source and destination colors).
    /// </summary>
    Additive,
    /// <summary>
    /// Opaque blending (standard for opaque objects).
    /// </summary>
    Opaque,
    /// <summary>
    /// No blending.
    /// </summary>
    NoBlend,
    /// <summary>
    /// No write to color buffer.
    /// </summary>
    NoWrite,
    /// <summary>
    /// Non-premultiplied alpha blending (preserves alpha for transparency).
    /// </summary>
    NonPremultipliedAlpha
}

/// <summary>
/// Represents culling modes for rendering.
/// </summary>
public enum CullMode
{
    /// <summary>
    /// No culling.
    /// </summary>
    None,
    /// <summary>
    /// Clockwise back-face culling.
    /// </summary>
    ClockwiseBack,
    /// <summary>
    /// Counter-clockwise back-face culling (default).
    /// </summary>
    CounterClockwiseBack,
    /// <summary>
    /// Clockwise front-face culling.
    /// </summary>
    ClockwiseFront,
    /// <summary>
    /// Counter-clockwise front-face culling.
    /// </summary>
    CounterClockwiseFront,
    /// <summary>
    /// Clockwise culling disabled.
    /// </summary>
    ClockwiseDisabled,
    /// <summary>
    /// Counter-clockwise culling disabled.
    /// </summary>
    CounterClockwiseDisabled
}

/// <summary>
/// Represents depth modes for rendering.
/// </summary>
public enum DepthMode
{
    /// <summary>
    /// Depth drawing for opaque objects.
    /// </summary>
    DrawOpaque,
    /// <summary>
    /// Depth drawing always.
    /// </summary>
    DrawAlways,
    /// <summary>
    /// Never draw depth.
    /// </summary>
    DrawNever,
    /// <summary>
    /// Depth pre-pass for alpha objects.
    /// </summary>
    PrepassAlpha,
    /// <summary>
    /// Depth testing disabled.
    /// </summary>
    TestDisabled
}

/// <summary>
/// Represents wireframe modes for rendering.
/// </summary>
public enum WireframeMode
{
    /// <summary>
    /// No wireframe rendering.
    /// </summary>
    None,
    /// <summary>
    /// Clockwise wireframe.
    /// </summary>
    Clockwise,
    /// <summary>
    /// Counter-clockwise wireframe.
    /// </summary>
    CounterClockwise
}