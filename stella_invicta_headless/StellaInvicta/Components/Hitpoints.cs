namespace StellaInvicta.Components;

/// <summary>
/// Represents a component that holds hitpoint value in the game.
/// </summary>
/// <param name="Value">The current hitpoint value.</param>
/// <remarks>
/// This is a record struct used to track entity health or durability in the game system.
/// The Value parameter represents the actual number of hitpoints.
/// </remarks>
public record struct Hitpoints(int Value);