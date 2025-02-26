namespace StellaInvicta.Components;

/// <summary>
/// Represents a two-dimensional velocity vector with X and Y components.
/// </summary>
/// <param name="X">The velocity component along the X axis.</param>
/// <param name="Y">The velocity component along the Y axis.</param>
/// <remarks>
/// This is a value type implemented as a readonly record struct, making it immutable and efficient for passing velocity data.
/// </remarks>
public record struct Velocity2D(float X, float Y);
