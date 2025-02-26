namespace StellaInvicta.Components;
/// <summary>
/// Represents a quantity of oil as a value type.
/// </summary>
/// <param name="Quantity">The amount of oil measured in units.</param>
/// <remarks>
/// This is an immutable record struct that stores oil quantity information.
/// </remarks>
public record struct Oil(int Quantity);
