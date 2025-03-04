namespace StellaInvicta.Components;


/// <summary>
/// Represents a workforce capacity as an immutable value type.
/// </summary>
/// <remarks>
/// WorkForce is implemented as a record struct for efficient handling of workforce-related calculations.
/// </remarks>
/// <param name="Value">The numerical value representing the workforce capacity. Defaults to 0 if not specified.</param>
public record struct WorkForce(int Value = 0);
