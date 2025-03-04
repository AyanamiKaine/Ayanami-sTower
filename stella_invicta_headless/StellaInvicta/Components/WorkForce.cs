namespace StellaInvicta.Components;


/// <summary>
/// Represents a workforce capacity as an immutable value type.
/// </summary>
/// <remarks>
/// WorkForce is implemented as a record struct for efficient handling of workforce-related calculations.
/// </remarks>
/// <param name="Value">The numerical value representing the workforce capacity. Defaults to 0 if not specified.</param>
public class WorkForce(int Value = 0)
{
    /// <summary>
    /// Gets or sets the current workforce value indicating the number of available workers.
    /// </summary>
    /// <value>
    /// An integer representing the workforce capacity.
    /// </value>
    public int Value { get; set; } = Value;
};
