namespace StellaInvicta.Components;

/// <summary>
/// Represents a diplomatic relationship between entities with a numerical value.
/// </summary>
/// <param name="Value">The numerical value representing the state of diplomatic relations. Defaults to 0.</param>
/// <remarks>
/// A positive value typically indicates friendly relations, while a negative value indicates hostile relations.
/// </remarks>
public record struct Diplomacy(double Value = 0);
