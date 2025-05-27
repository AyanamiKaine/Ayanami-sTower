using Flecs.NET.Core;

namespace AyanamisTower.StellaInvicta.Core;

/// <summary>
/// Represents the main entry point for any stella invicta game
/// </summary>
public class GameServer
{
    /// <summary>
    /// Main ECS game world, here are all game related entities and systems
    /// </summary>
    public World World { get; } = World.Create();
    /// <summary>
    /// Gets the current tick of the game.
    /// </summary>
    public int CurrentTick { get; private set; }
    /// <summary>
    /// Ticks per second (TPS) defines how many times the internal game state 
    /// should be updated every second. A TPS of 10 means that every 100ms the
    /// game world updates
    /// </summary>
    public TPS TicksPerSecond { get; set; } = new(10);
    /// <summary>
    /// runs up the game loop
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task GameLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Game Simulation
                World.Progress();
                Console.WriteLine(World.ToJson());
                CurrentTick++;
                Console.WriteLine($"Current Tick {CurrentTick}");
                await Task.Delay(TicksPerSecond.ToMilliseconds(), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error in async loop: {ex.Message}");
            }
        }
    }
}
