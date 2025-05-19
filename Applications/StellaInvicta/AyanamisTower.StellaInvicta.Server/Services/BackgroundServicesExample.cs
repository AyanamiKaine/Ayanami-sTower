using System;
using Flecs.NET.Core;

namespace AyanamisTower.StellaInvicta.Server.Services;

// GameState.cs (A simple example of shared game state)
// IMPORTANT: Ensure this is thread-safe if accessed by multiple services/threads.
// For simplicity, this example uses a basic lock.
/// <summary>
/// Consider more advanced thread-safe collections or patterns for complex state.
/// </summary>
public class GameState
{
    private readonly Lock _lock = new();
    private int _tickCount;
    private DateTime _lastUpdate;
    private readonly World _world = World.Create();
    /// <summary>
    /// EXAMPLE
    /// </summary>
    public void UpdateState()
    {
        lock (_lock)
        {
            _tickCount++;
            _lastUpdate = DateTime.UtcNow;
            // TODO: Add your actual game state update logic here
            // e.g., move players, update objects, check conditions
            Console.WriteLine($"Game Loop Tick: {_tickCount}, Last Update: {_lastUpdate}");
            _world.Progress();
        }
    }

    /// <summary>
    /// EXAMPLE
    /// </summary>
    public string GetCurrentStatus()
    {
        lock (_lock)
        {
            return $"Server Tick: {_tickCount}, Last Game Update: {_lastUpdate}";
        }
    }

    /// <summary>
    /// EXAMPLE
    /// </summary>
    public int GetTickCount()
    {
        lock (_lock)
        {
            return _tickCount;
        }
    }
}

/// <summary>
/// (The IHostedService implementation)
/// </summary>
public class GameLoopService : IHostedService, IDisposable
{
    private readonly ILogger<GameLoopService> _logger;
    private readonly GameState _gameState; // Injected game state
    //private Timer? _timer;
    private Task? _executingTask;
    private readonly CancellationTokenSource _stoppingCts = new();
    private const int TickRateMilliseconds = 1000; // Roughly 60 FPS/TPS

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="gameState"></param>
    public GameLoopService(ILogger<GameLoopService> logger, GameState gameState)
    {
        _logger = logger;
        _gameState = gameState; // Inject the shared game state
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Game Loop Service is starting.");

        // Store the task we're executing
        _executingTask = ExecuteAsync(_stoppingCts.Token);

        // If the task is completed then return it,
        // this will bubble cancellation and failure to the caller
        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        // Otherwise it's running
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game Loop executing.");

        // Loop until the cancellation token is triggered
        while (!stoppingToken.IsCancellationRequested)
        {
            _gameState.UpdateState(); // Perform one tick of game logic

            try
            {
                // Wait for the next tick, respecting the cancellation token
                await Task.Delay(TickRateMilliseconds, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if Task.Delay throws during stop
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in game loop.");
            }
        }

        _logger.LogInformation("Game Loop is stopping.");
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Game Loop Service is stopping.");

        if (_executingTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            _stoppingCts.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        _logger.LogInformation("Game Loop Service has stopped.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stoppingCts.Cancel();
        _stoppingCts.Dispose();
        //_timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}