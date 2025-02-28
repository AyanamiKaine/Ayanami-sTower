namespace StellaInvicta.Components;


/// <summary>
/// Represents a universally used value type for handling in-game currency (Credits).
/// </summary>
/// <remarks>
/// Credits are implemented as a record struct for efficient value-based operations and immutability.
/// </remarks>
/// <param name="Ammount">The numerical value of credits. Defaults to 0 if not specified.</param>
public record struct Credits(float Ammount = 0);
