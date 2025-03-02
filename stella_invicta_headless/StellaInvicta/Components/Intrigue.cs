namespace StellaInvicta.Components;

/// <summary>
/// Represents an intrigue stats similar to how its done in Ck2/3
/// </summary>
/// <remarks>
/// Intrigue is implemented as a record struct for performance and immutability.
/// </remarks>
/// <param name="Value">The numerical value representing the intrigue level. Defaults to 0.</param>
public record struct Intrigue(double Value = 0);
