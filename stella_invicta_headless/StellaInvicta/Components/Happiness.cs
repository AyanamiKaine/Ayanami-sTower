namespace StellaInvicta.Components;

using System;

/// <summary>
/// Overall satisfaction and well-being
/// </summary>
public record struct Happiness
{
    private float _value;
    
    /// <summary>
    /// Gets or sets the happiness value, representing the level as a normalized float between 0 and 1.
    /// </summary>
    /// <value>A float value between 0 (no happiness) and 1 (maximum happiness).</value>
    public float Value
    {
        readonly get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets the happiness percentage, representing the level as a percentage between 0 and 100.
    /// </summary>
    /// <value>A float value between 0 (no happiness) and 100 (maximum happiness).</value>
    public readonly float Percentage => Value * 100f;

    /// <summary>
    /// Initializes a new instance of the Happiness class with a specified value.
    /// </summary>
    /// <param name="value">The initial happiness value as a normalized float between 0 and 1. Defaults to 0 if not specified.</param>
    public Happiness(float value = 0) : this()
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Happiness instance from a percentage value.
    /// </summary>
    /// <param name="percentage">The happiness percentage (0-100)</param>
    /// <returns>A new Happiness instance</returns>
    public static Happiness FromPercentage(float percentage)
    {
        return new Happiness(percentage / 100f);
    }
}