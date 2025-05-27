using System.Diagnostics;
using Flecs.NET.Core;

namespace AyanamisTower.NihilEx;

/// <summary>
/// Represents the main entry point in setting up NihilEx,
/// The core represents the main loop of the ECS world with 
/// the least amount of external dependencies.
/// </summary>
public class Core
{
    /// <summary>
    /// Main ECS world
    /// </summary>
    public World World { get; } = World.Create();

    /// <summary>
    /// How many times the systems of a world should run per second
    /// 10 ticks per second means that a world progresses approximately
    /// every 100ms.
    /// </summary>
    public float TicksPerSecond { get; set; } = 10.0f;

    /// <summary>
    /// Current framecount, gets incremented everytime one tick
    /// progessives
    /// </summary>
    public int FrameCount { get; private set; }

    /// <summary>
    /// Get current delta time in seconds
    /// </summary>
    public float DeltaTime => World.DeltaTime();
    private Stopwatch GameTimer { get; } = new();

    /// <summary>
    /// Runs the game simulation as fast as the defined ticks per second
    /// and yields after every tick so it does not block.
    /// </summary>
    /// <returns></returns>
    public async Task Run()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.100 - (DeltaTime - 0.100)));

            GameTimer.Start();

            World.Progress();

            GameTimer.Stop();

            FrameCount++;
            Console.WriteLine($"FrameCount: {FrameCount}, DeltaTime: {DeltaTime}");
            await Task.Yield();
        }
    }
}
