namespace StellaInvicta.Components;

/// <summary>
/// Represents a Quantity value as an integer that's never negative.
/// </summary>
public record struct Quantity
{
    private int _value; // Removed readonly
    
    /// <summary>
    /// Gets or sets the current numeric value of the quantity.
    /// </summary>
    /// <value>The integer value representing this quantity.</value>
    /// <remarks>Setting a negative value will result in a value of 0.</remarks>
    public int Value 
    { 
        get => _value;
        set => _value = Math.Max(0, value);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Quantity"/> class.
    /// </summary>
    /// <param name="value">The initial quantity value. Default is 0.</param>
    /// <remarks>The quantity value will never be negative. If a negative value is provided, it will be set to 0.</remarks>
    public Quantity(int value = 0)
    {
        _value = Math.Max(0, value);
    }
}
