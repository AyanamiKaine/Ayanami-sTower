namespace StellaInvicta.Components;

using System;

/// <summary>
/// Represents pop political awareness and desire for reform.
///
/// Driven by events and ideologies: Events, national foci, and ideologies spread by clergy and intellectuals increase consciousness.
/// Influences Political Movements: High consciousness makes pops more likely to support political movements and demand reforms (e.g., voting rights, social reforms).
/// </summary>
public record struct Consciousness
{
    private float _value;
    
    /// <summary>
    /// Gets or sets the consciousness value, representing the level as a normalized float between 0 and 1.
    /// </summary>
    /// <value>A float value between 0 (no consciousness) and 1 (maximum consciousness).</value>
    public float Value
    {
        readonly get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets the consciousness percentage, representing the level as a percentage between 0 and 100.
    /// </summary>
    /// <value>A float value between 0 (no consciousness) and 100 (maximum consciousness).</value>
    public readonly float Percentage => Value * 100f;

    /// <summary>
    /// Initializes a new instance of the Consciousness class with a specified value.
    /// </summary>
    /// <param name="value">The initial consciousness value as a normalized float between 0 and 1. Defaults to 0 if not specified.</param>
    public Consciousness(float value = 0) : this()
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Consciousness instance from a percentage value.
    /// </summary>
    /// <param name="percentage">The consciousness percentage (0-100)</param>
    /// <returns>A new Consciousness instance</returns>
    public static Consciousness FromPercentage(float percentage)
    {
        return new Consciousness(percentage / 100f);
    }
}
