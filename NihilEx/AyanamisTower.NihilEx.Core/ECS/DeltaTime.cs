using System.Diagnostics;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Manages delta time calculation for the application loop.
/// Provides access to the time elapsed since the last frame and total elapsed time.
/// </summary>
public class DeltaTime
{
    /// <summary>
    /// Gets the time elapsed since the last call to Update, in seconds.
    /// </summary>
    public float DeltaSeconds { get; private set; }

    /// <summary>
    /// Gets the total time elapsed since Initialize was called, in seconds.
    /// </summary>
    public float TotalSeconds { get; private set; }

    /// <summary>
    /// Initializes the timer. Call this once at the start of the application.
    /// </summary>
    public void Initialize()
    {
        TotalSeconds = 0;
    }

    /// <summary>
    /// Updates the delta time. Call this once per frame/iteration, typically at the beginning.
    /// </summary>
    public void Update()
    {
    }
}