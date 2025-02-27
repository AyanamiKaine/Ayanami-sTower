namespace StellaInvicta.Components;

/// <summary>
/// Represents a long description as a record struct.
/// </summary>
/// <param name="Value">The string value containing the long description. Defaults to empty string if not specified.</param>
public record struct LongDescription(string Value = "");
