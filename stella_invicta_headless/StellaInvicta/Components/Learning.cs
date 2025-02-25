namespace StellaInvicta.Components;

/// <summary>
/// Represents a component that tracks learning or skill development progress.
/// </summary>
/// <param name="Value">The current learning value or progress level, defaults to 0.</param>
/// <remarks>
/// This is an immutable record struct used to store learning-related data.
/// The Value parameter represents the accumulated learning or skill level,
/// where higher values indicate greater progress or mastery.
/// </remarks>
public record struct Learning(double Value = 0);
