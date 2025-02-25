namespace StellaInvicta.Components;

/// <summary>
/// Represents an immutable record structure for storing a name.
/// </summary>
/// <param name="Value">The name value to be stored.</param>
/// <remarks>
/// This record struct provides a lightweight, immutable way to encapsulate a name value.
/// Being a record struct, it automatically implements value equality and other useful features.
/// </remarks>
public record struct Name(string Value);