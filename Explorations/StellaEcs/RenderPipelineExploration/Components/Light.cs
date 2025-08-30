using System.Numerics;

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
}

/// <summary>
/// Represents a point light that shines from a specific position in all directions.
/// Point lights have a range and fall off with distance.
/// </summary>
public struct PointLight
{
    /// <summary>
    /// The position of the light.
    /// </summary>
    public Vector3 Position;
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
    /// Initializes a new instance of the <see cref="PointLight"/> struct.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="range"></param>
    public PointLight(Vector3 position, Vector3 color, float intensity, float range)
    {
        Position = position;
        Color = color;
        Intensity = intensity;
        Range = range;
    }
}

/// <summary>
/// Represents a spot light that shines from a position in a cone shape.
/// </summary>
public struct SpotLight
{
    /// <summary>
    /// The position of the light.
    /// </summary>
    public Vector3 Position;
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
    /// Initializes a new instance of the <see cref="SpotLight"/> struct.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <param name="color"></param>
    /// <param name="intensity"></param>
    /// <param name="range"></param>
    /// <param name="innerAngle"></param>
    /// <param name="outerAngle"></param>
    public SpotLight(Vector3 position, Vector3 direction, Vector3 color, float intensity, float range, float innerAngle, float outerAngle)
    {
        Position = position;
        Direction = direction;
        Color = color;
        Intensity = intensity;
        Range = range;
        InnerAngle = innerAngle;
        OuterAngle = outerAngle;
    }
}
