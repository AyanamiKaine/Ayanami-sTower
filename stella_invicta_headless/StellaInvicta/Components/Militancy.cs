namespace StellaInvicta.Components;

using System;

/// <summary>
/// Represents pop dissatisfaction and anger.
///
/// Driven by Unmet Needs: Lack of goods, high taxes, unemployment, war exhaustion, and negative events increase militancy.
/// Leads to Unrest and Rebellions: High militancy can lead to protests, strikes, and eventually, armed rebellions.
/// </summary>
public record struct Militancy
{
    private float _value;
    
    /// <summary>
    /// Gets or sets the militancy value, representing the level as a normalized float between 0 and 1.
    /// </summary>
    /// <value>A float value between 0 (no militancy) and 1 (maximum militancy).</value>
    public float Value
    {
        readonly get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets the militancy percentage, representing the level as a percentage between 0 and 100.
    /// </summary>
    /// <value>A float value between 0 (no militancy) and 100 (maximum militancy).</value>
    public readonly float Percentage => Value * 100f;

    /// <summary>
    /// Initializes a new instance of the Militancy class with a specified value.
    /// </summary>
    /// <param name="value">The initial militancy value as a normalized float between 0 and 1. Defaults to 0 if not specified.</param>
    public Militancy(float value = 0) : this()
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Militancy instance from a percentage value.
    /// </summary>
    /// <param name="percentage">The militancy percentage (0-100)</param>
    /// <returns>A new Militancy instance</returns>
    public static Militancy FromPercentage(float percentage)
    {
        return new Militancy(percentage / 100f);
    }
}
