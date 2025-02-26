namespace StellaInvicta.Components;


/// <summary>
/// Represents a mass value in kilograms as an immutable struct.
/// </summary>
/// <param name="Value">The mass value in kilograms. Defaults to 0 if not specified.</param>
/// <remarks>
/// This is a record struct that holds a single float value representing mass.
/// The struct is immutable and implements value-based equality comparisons.
/// </remarks>
public record struct Mass(float Value = 0);