namespace StellaInvicta.Components;

/// <summary>
/// Represents the percentage of the pop that can read and write.
/// </summary>
public record struct Literacy
{
    private double _value;
    /// <summary>
    /// Gets or sets the literacy value, representing the proficiency level as a normalized float between 0 and 1.
    /// </summary>
    /// <value>A float value between 0 (completely illiterate) and 1 (fully literate).</value>
    public double Value
    {
        readonly get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets the literacy percentage, representing the proficiency level as a percentage between 0 and 100.
    /// </summary>
    /// <value>A float value between 0 (completely illiterate) and 100 (fully literate).</value>
    public readonly double Percentage => Value * 100f;

    /// <summary>
    /// Initializes a new instance of the Literacy class with a specified value.
    /// </summary>
    /// <param name="value">The initial literacy value as a normalized float between 0 and 1. Defaults to 0 if not specified.</param>
    public Literacy(double value = 0) : this()
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Literacy instance from a percentage value.
    /// </summary>
    /// <param name="percentage">The literacy percentage (0-100)</param>
    /// <returns>A new Literacy instance</returns>
    public static Literacy FromPercentage(double percentage)
    {
        return new Literacy(percentage / 100f);
    }
}
