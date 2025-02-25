namespace StellaInvicta.Components;

/// <summary>
/// Represents an intrigue value in the game system.
/// </summary>
/// <remarks>
/// Intrigue is implemented as a record struct for performance and immutability.
/// </remarks>
/// <param name="Value">The numerical value representing the intrigue level. Defaults to 0.</param>
public record struct Intrigue(double Value = 0);
