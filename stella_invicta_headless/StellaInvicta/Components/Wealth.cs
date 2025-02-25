namespace StellaInvicta.Components;

/// <summary>
/// Represents a record struct for storing wealth value.
/// </summary>
/// <param name="Value">The numerical value representing wealth. Defaults to 0 if not specified.</param>
/// <remarks>
/// This struct is immutable and provides value-based equality comparisons.
/// </remarks>
public record struct Wealth(double Value = 0);
