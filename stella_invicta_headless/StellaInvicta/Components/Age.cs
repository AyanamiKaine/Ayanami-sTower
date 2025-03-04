namespace StellaInvicta.Components;

// TODO: Often times we simply define a value object, but would like to add various more things 
// like operators. Maybe a look into https://github.com/SteveDunn/Vogen is worth. 
// It Source generates Value Objects.

/// <summary>
/// Represents a value type that stores an age value.
/// </summary>
/// <param name="Value">The numerical age value. Defaults to 0 if not specified.</param>
/// <remarks>
/// This is implemented as a record struct for efficient value-based age representation with built-in value equality.
/// </remarks>
public record struct Age(int Value = 0);
