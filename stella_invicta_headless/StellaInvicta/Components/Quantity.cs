namespace StellaInvicta.Components;

/// <summary>
/// Represents a Quantity value as an integer.
/// </summary>
/// <param name="Value">The size value. Defaults to 0 if not specified.</param>
public record struct Quantity(int Value = 0);
