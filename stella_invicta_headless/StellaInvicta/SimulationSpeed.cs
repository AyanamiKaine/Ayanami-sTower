namespace StellaInvicta;

/// <summary>
/// Contains constant values for different simulation speed settings.
/// </summary>
public static class SimulationSpeed
{
    /// <summary>
    /// Represents a fast simulation speed multiplier
    /// </summary>
    /// <remarks>
    /// Any System having the VeryFast speed runs 8 times a second
    /// </remarks>
    public const float ReallyVeryFast = Default / 8;

    /// <summary>
    /// Represents a fast simulation speed multiplier, set to 1.5x the default speed.
    /// </summary>
    /// <remarks>
    /// Any System having the VeryFast speed runs 4 times a second
    /// </remarks>
    public const float VeryFast = Default / 4;
    /// <summary>
    /// Represents a fast simulation speed multiplier, set to double the default speed.
    /// </summary>
    public const float Fast = Default / 2;
    /// <summary>
    /// Represents a Default simulation speed multiplier, one update every second.
    /// </summary>
    /// <remarks>
    /// Any System having the default speed runs every second
    /// </remarks>
    public const float Default = 1.0f;
    /// <summary>
    /// Represents a slow simulation speed multiplier, set to 0.25x the default speed.
    /// </summary>
    /// <remarks>
    /// Any System having the slow speed runs every one and a half second
    /// </remarks>
    public const float Slow = Default * 2;
    /// <summary>
    /// Represents a very slow simulation speed multiplier, set to 0.5x the default speed.
    /// </summary>
    /// <remarks>
    /// Any System having the very slow speed runs every two seconds.
    /// </remarks>
    public const float VerySlow = Default * 4;
    /// <summary>
    /// Runs the simulation as fast as your pc allows
    /// </summary>
    /// <remarks>
    /// Any System having the unlocked speed runs as fast as possible.
    /// </remarks>
    public const float Unlocked = 0;
}