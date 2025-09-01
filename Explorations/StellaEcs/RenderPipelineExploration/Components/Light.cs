using System.Numerics;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;

/// <summary>
/// Represents a directional light that shines in a specific direction.
/// Directional lights are typically used for sunlight.
/// </summary>
public struct DirectionalLight
{
    /// <summary>
    /// The direction the light is shining.
    /// </summary>
    public Vector3 Direction;
    /// <summary>
    /// The color of the light.
    /// </summary>
    public Vector3 Color;
    /// <summary>
    /// The intensity of the light.
    /// </summary>
    public float Intensity;
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectionalLight"/> struct.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    public DirectionalLight(Vector3 direction, Vector3 color, float intensity)
    {
        Direction = direction;
        Color = color;
        Intensity = intensity;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectionalLight"/> struct using a MoonWorks Color.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    public DirectionalLight(Vector3 direction, Color color, float intensity)
    {
        Direction = direction;
        Color = new Vector3(color.R, color.G, color.B);
        Intensity = intensity;
    }
}

/// <summary>
/// Represents a point light that shines from a specific position in all directions.
/// Point lights have a range and fall off with distance.
/// </summary>
public struct PointLight
{
    /// <summary>
    /// The color of the light.
    /// </summary>
    public Vector3 Color;
    /// <summary>
    /// The intensity of the light.
    /// </summary>
    public float Intensity;
    /// <summary>
    /// The range of the light.
    /// </summary>
    public float Range;
    /// <summary>
    /// Attenuation constant term Kc. Usually 1.0.
    /// </summary>
    public float Constant;
    /// <summary>
    /// Attenuation linear term Kl. Typical values ~0.07 - 0.027 for mid/long range.
    /// </summary>
    public float Linear;
    /// <summary>
    /// Attenuation quadratic term Kq. Typical values ~0.017 - 0.0028 for mid/long range.
    /// </summary>
    public float Quadratic;
    /// <summary>
    /// Initializes a new instance of the <see cref="PointLight"/> struct.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="range"></param>
    public PointLight(Vector3 color, float intensity, float range)
    {
        Color = color;
        Intensity = intensity;
        Range = range;
        // Sensible default attenuation roughly matching ~50 units effective range from the table
        Constant = 1.0f;
        Linear = 0.09f;
        Quadratic = 0.032f;
    }

    /// <summary>
    /// Initializes a new instance including explicit attenuation terms.
    /// </summary>
    public PointLight(Vector3 color, float intensity, float range, float constant, float linear, float quadratic)
    {
        Color = color;
        Intensity = intensity;
        Range = range;
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PointLight"/> struct using a MoonWorks Color.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="range"></param>
    public PointLight(Color color, float intensity, float range)
    {
        Color = new Vector3(color.R, color.G, color.B);
        Intensity = intensity;
        Range = range;
        Constant = 1.0f;
        Linear = 0.09f;
        Quadratic = 0.032f;
    }

    /// <summary>
    /// Initializes a new instance using a Color and explicit attenuation terms.
    /// </summary>
    public PointLight(Color color, float intensity, float range, float constant, float linear, float quadratic)
    {
        Color = new Vector3(color.R, color.G, color.B);
        Intensity = intensity;
        Range = range;
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
    }
}

/// <summary>
/// Represents a spot light that shines from a position in a cone shape.
/// </summary>
public struct SpotLight
{
    /// <summary>
    /// The direction the light is shining.
    /// </summary>
    public Vector3 Direction;
    /// <summary>
    /// The color of the light.
    /// </summary>
    public Vector3 Color;
    /// <summary>
    /// The intensity of the light.
    /// </summary>
    public float Intensity;
    /// <summary>
    /// The range of the light.
    /// </summary>
    public float Range;
    /// <summary>
    /// The inner angle of the light cone.
    /// </summary>
    public float InnerAngle;
    /// <summary>
    /// The outer angle of the light cone.
    /// </summary>
    public float OuterAngle;
    /// <summary>
    /// Attenuation constant term Kc. Usually 1.0.
    /// </summary>
    public float Constant;
    /// <summary>
    /// Attenuation linear term Kl.
    /// </summary>
    public float Linear;
    /// <summary>
    /// Attenuation quadratic term Kq.
    /// </summary>
    public float Quadratic;
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotLight"/> struct.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="range"></param>
    /// <param name="innerAngle"></param>
    /// <param name="outerAngle"></param>
    public SpotLight(Vector3 direction, Vector3 color, float intensity, float range, float innerAngle, float outerAngle)
    {
        Direction = direction;
        Color = color;
        Intensity = intensity;
        Range = range;
        InnerAngle = innerAngle;
        OuterAngle = outerAngle;
        // Defaults similar to point light with ~50 unit effective range
        Constant = 1.0f;
        Linear = 0.09f;
        Quadratic = 0.032f;
    }

    /// <summary>
    /// Initializes a new instance including explicit attenuation terms.
    /// </summary>
    public SpotLight(Vector3 direction, Vector3 color, float intensity, float range, float innerAngle, float outerAngle, float constant, float linear, float quadratic)
    {
        Direction = direction;
        Color = color;
        Intensity = intensity;
        Range = range;
        InnerAngle = innerAngle;
        OuterAngle = outerAngle;
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotLight"/> struct using a MoonWorks Color.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="range"></param>
    /// <param name="innerAngle"></param>
    /// <param name="outerAngle"></param>
    public SpotLight(Vector3 direction, Color color, float intensity, float range, float innerAngle, float outerAngle)
    {
        Direction = direction;
        Color = new Vector3(color.R, color.G, color.B);
        Intensity = intensity;
        Range = range;
        InnerAngle = innerAngle;
        OuterAngle = outerAngle;
        Constant = 1.0f;
        Linear = 0.09f;
        Quadratic = 0.032f;
    }

    /// <summary>
    /// Initializes a new instance using a Color and explicit attenuation terms.
    /// </summary>
    public SpotLight(Vector3 direction, Color color, float intensity, float range, float innerAngle, float outerAngle, float constant, float linear, float quadratic)
    {
        Direction = direction;
        Color = new Vector3(color.R, color.G, color.B);
        Intensity = intensity;
        Range = range;
        InnerAngle = innerAngle;
        OuterAngle = outerAngle;
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
    }
}
