using AyanamisTower.Utilities.Aspects;
using SDL3;

namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Manages delta time calculation for the application loop.
/// Provides access to the time elapsed since the last frame and total elapsed time.
/// </summary>
[PrettyPrint]
public class DeltaTime
{
    private ulong _startTimeTicks;
    private ulong _lastUpdateTimeTicks;
    private ulong _frequency; // Frequency for high-performance counter

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
        // Use high-performance counter if available for better precision
        _frequency = SDL.SDL_GetPerformanceFrequency();
        _startTimeTicks = SDL.SDL_GetPerformanceCounter();
        _lastUpdateTimeTicks = _startTimeTicks; // Initialize last update time
        DeltaSeconds = 0.0f;
        TotalSeconds = 0.0f;
        //SDL.LogInfo(SDL.LogCategory.Application, $"DeltaTime Initialized. Frequency: {_frequency}");
    }

    /// <summary>
    /// Updates the delta time. Call this once per frame/iteration, typically at the beginning.
    /// </summary>
    public void Update()
    {
        ulong currentTimeTicks = SDL.SDL_GetPerformanceCounter();

        // Calculate delta time
        ulong elapsedTicks = currentTimeTicks - _lastUpdateTimeTicks;
        DeltaSeconds = (float)elapsedTicks / _frequency;

        // Calculate total elapsed time
        ulong totalElapsedTicks = currentTimeTicks - _startTimeTicks;
        TotalSeconds = (float)totalElapsedTicks / _frequency;

        // Update last time for the next frame
        _lastUpdateTimeTicks = currentTimeTicks;
    }
}