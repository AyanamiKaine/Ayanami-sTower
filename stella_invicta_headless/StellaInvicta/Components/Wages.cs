namespace StellaInvicta.Components;

/// <summary>
/// Represents a wage for one pop, the scaling works like this
/// if pops with the size 1000 are employed they are paid 1000 * wages
/// </summary>
/// <param name="Value"></param>
public record struct Wages(int Value = 0);
