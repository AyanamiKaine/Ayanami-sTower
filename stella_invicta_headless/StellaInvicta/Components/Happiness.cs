namespace StellaInvicta.Components;

using System;

/// <summary>
/// Overall satisfaction and well-being
/// </summary>
public record struct Happiness
{
    private double _value;

    /// <summary>
    /// Gets or sets the happiness value, representing the level as a normalized double between 0 and 1.
    /// </summary>
    /// <value>A double value between 0 (no happiness) and 1 (maximum happiness).</value>
    public double Value
    {
        readonly get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets the happiness percentage, representing the level as a percentage between 0 and 100.
    /// </summary>
    /// <value>A double value between 0 (no happiness) and 100 (maximum happiness).</value>
    public readonly double Percentage => Value * 100f;

    /// <summary>
    /// Initializes a new instance of the Happiness class with a specified value.
    /// </summary>
    /// <param name="value">The initial happiness value as a normalized double between 0 and 1. Defaults to 0 if not specified.</param>
    public Happiness(double value = 0) : this()
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Happiness instance from a percentage value.
    /// </summary>
    /// <param name="percentage">The happiness percentage (0-100)</param>
    /// <returns>A new Happiness instance</returns>
    public static Happiness FromPercentage(double percentage)
    {
        return new Happiness(percentage / 100f);
    }
}