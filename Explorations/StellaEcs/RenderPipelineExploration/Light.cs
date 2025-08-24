using System.Numerics;

namespace AyanamisTower.StellaEcs.StellaInvicta;


/// <summary>
/// Base class for all light types.
/// </summary>
public abstract class Light
{
    /// <summary>
    /// Whether the light is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The color of the light.
    /// </summary>
    // RGB in 0..1
    public Vector3 Color { get; set; } = Vector3.One;

    /// <summary>
    /// Scalar multiplier for brightness
    /// </summary>
    public float Intensity { get; set; } = 1f;
}
/// <summary>
/// Represents a directional light source.
/// </summary>
public sealed class DirectionalLight : Light
{
    Vector3 direction = Vector3.Normalize(new Vector3(0, -1, 0));
    /// <summary>
    /// The direction of the light.
    /// </summary>
    public Vector3 Direction
    {
        get => direction;
        set => direction = value.LengthSquared() > 0 ? Vector3.Normalize(value) : direction;
    }
}
/// <summary>
/// Represents a point light source.
/// </summary>
public class PointLight : Light
{
    /// <summary>
    /// The position of the light.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    // For range-based falloff; treat as radius where contribution ~0.
    /// <summary>
    /// The range of the light.
    /// </summary>
    public float Range { get; set; } = 10f;

    // Optional physically-inspired attenuation (1 / (1 + lin*d + quad*d^2))
    /// <summary>
    /// Linear attenuation factor.
    /// </summary>
    public float AttenuationLinear { get; set; } = 0f;
    /// <summary>
    /// Quadratic attenuation factor.
    /// </summary>
    public float AttenuationQuadratic { get; set; } = 1f;
}
/// <summary>
/// Represents a spotlight light source.
/// </summary>
public class SpotLight : PointLight
{
    /// <summary>
    /// The direction of the light.
    /// </summary>
    Vector3 direction = Vector3.UnitZ;
    /// <summary>
    /// The direction of the light.
    /// </summary>
    public Vector3 Direction
    {
        get => direction;
        set => direction = value.LengthSquared() > 0 ? Vector3.Normalize(value) : direction;
    }

    // Half-angles in radians (inner fully lit, outer fully dark; smoothstep between)
    float innerHalfAngle = MathF.PI / 16f;
    float outerHalfAngle = MathF.PI / 8f;
    /// <summary>
    /// The inner half-angle of the spotlight.
    /// </summary>
    public float InnerHalfAngle
    {
        get => innerHalfAngle;
        set => innerHalfAngle = Math.Clamp(value, 0f, outerHalfAngle);
    }
    /// <summary>
    /// The outer half-angle of the spotlight.
    /// </summary>
    public float OuterHalfAngle
    {
        get => outerHalfAngle;
        set => outerHalfAngle = Math.Max(value, innerHalfAngle);
    }

    // Cached cosines for shaders (cheaper to compare)
    /// <summary>
    /// The cached cosine of the inner half-angle.
    /// </summary>
    public float InnerCos => MathF.Cos(innerHalfAngle);
    /// <summary>
    /// The cached cosine of the outer half-angle.
    /// </summary>
    public float OuterCos => MathF.Cos(outerHalfAngle);
}

/// <summary>
/// Convenience: a spotlight that follows a pose (e.g., camera)
/// </summary>
public sealed class Flashlight : SpotLight
{
    /// <summary>
    /// Update from a world-space pose (position + forward direction)
    /// </summary>
    public void UpdateFromPose(Vector3 position, Vector3 forward)
    {
        Position = position;
        Direction = forward;
    }
}