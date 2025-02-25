namespace StellaInvicta.Components;

/// <summary>
/// Represents a measurement of stewardship capability or effectiveness.
/// </summary>
/// <remarks>
/// Stewardship is implemented as a readonly record struct for efficient value-based operations.
/// </remarks>
/// <param name="Value">The numerical value representing the stewardship level. Defaults to 0.</param>
public record struct Stewardship(double Value = 0);
