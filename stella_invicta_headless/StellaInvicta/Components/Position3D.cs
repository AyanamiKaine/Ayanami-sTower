namespace StellaInvicta.Components;

/// <summary>
/// Represents a three-dimensional position in space using floating-point coordinates.
/// </summary>
/// <param name="X">The position along the X axis.</param>
/// <param name="Y">The position along the Y axis.</param>
/// <param name="Z">The position along the Z axis.</param>
/// <remarks>
/// This is an immutable value type implemented as a record struct.
/// </remarks>
public record struct Position3D(float X, float Y, float Z);
