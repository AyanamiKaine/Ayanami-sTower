using System;

namespace AyanamisTower.StellaInvicta.Server;

/// <summary>
/// Used to define the gamespeed per tick.
/// The idea is the following imagine a month
/// of game time being calculated per game tick
/// on very fast, on normal only 15 days would
/// be calculated per tick.
/// </summary>
public enum GameSpeed
{
    /// <summary>
    /// Per game tick no simulations are processed
    /// </summary>
    Paused,
    /// <summary>
    /// Trying to process X game days per game tick
    /// </summary>
    Slow,
    /// <summary>
    /// Trying to process X game days per game tick
    /// </summary>
    Normal,
    /// <summary>
    /// Trying to process X game days per game tick
    /// </summary>
    Fast,
    /// <summary>
    /// Trying to process X game days per game tick
    /// </summary>
    VeryFast
}
