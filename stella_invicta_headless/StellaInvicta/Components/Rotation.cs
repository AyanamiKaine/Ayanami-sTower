namespace StellaInvicta.Components;

/// <summary>
/// Represents a rotation component with speed and angle values.
/// </summary>
/// <param name="Speed">The rotation speed in radians per second.</param>
/// <param name="Angle">The current angle in radians.</param>
/// <remarks>
/// This is a value type (struct) record that stores rotation information.
/// The Speed parameter determines how fast the rotation occurs.
/// The Angle parameter represents the current rotational position.
/// </remarks>
public record struct Rotation(float Speed, float Angle);