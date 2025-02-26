namespace StellaInvicta.Components;

/// <summary>
/// Represents a three-dimensional velocity vector with X, Y, and Z components.
/// </summary>
/// <param name="X">The velocity component along the X axis.</param>
/// <param name="Y">The velocity component along the Y axis.</param>
/// <param name="Z">The velocity component along the Z axis.</param>
/// <remarks>
/// This is a value type implemented as a readonly record struct, making it immutable and efficient for passing velocity data.
/// </remarks>
public record struct Velocity3D(float X, float Y, float Z);
