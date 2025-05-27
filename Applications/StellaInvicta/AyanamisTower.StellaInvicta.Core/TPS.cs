using System;

namespace AyanamisTower.StellaInvicta.Core;

/// <summary>
/// Represents ticks per second, with easy convertion methods to milliseconds, etc.
/// </summary>
public class TPS(float value = 10)
{
    private float _value = value;  // Backing store


    /// <summary>
    /// Current TicksPerSecond value, it can never be lower than 
    /// 0,
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            if (value >= 0)
                _value = value;
        }
    }



    /// <summary>
    /// Converts tps to milliseconds
    /// </summary>
    public int ToMilliseconds()
    {
        return (int)((1 / Value) * 1000);
    }
}

/// <summary>
/// Extension methods that work with TPS
/// </summary>
public static class TPSExtensions
{
    /// <summary>
    /// Creates a task that completes after a specified number of ticks per second.
    /// </summary>
    /// <param name="task"></param>
    /// <param name="tps"></param>
    /// <returns></returns>
    public static Task Delay(this Task task, TPS tps)
    {
        return Task.Delay(tps.ToMilliseconds());
    }
}
