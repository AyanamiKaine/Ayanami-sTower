namespace StellaInvicta.Components;

/// <summary>
/// Represents a martial attribute or characteristic.
/// </summary>
/// <param name="Value">The numeric value representing martial ability or prowess. Defaults to 0.</param>
/// <remarks>
/// This structure can be used to track and manage martial-related attributes in a game or simulation system.
/// The value represents the level or degree of martial capability, where higher values typically indicate
/// greater proficiency.
/// </remarks>
public record struct Martial(double Value = 0);
