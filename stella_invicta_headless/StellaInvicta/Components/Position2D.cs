namespace StellaInvicta.Components;

/// <summary>
/// Represents a 2D position in a coordinate system using floating-point values.
/// </summary>
/// <param name="X">The horizontal coordinate value.</param>
/// <param name="Y">The vertical coordinate value.</param>
/// <remarks>
/// This is an immutable value type implemented as a record struct, providing efficient storage and comparison of 2D positions.
/// </remarks>
public record struct Position2D(float X, float Y);
