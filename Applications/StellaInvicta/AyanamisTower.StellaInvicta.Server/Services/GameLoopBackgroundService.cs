using System;
using Flecs.NET.Core;

namespace AyanamisTower.StellaInvicta.Server.Services;
/// <summary>
/// Implements the gameloop that runs in the background
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="logger"></param>
public class GameLoopBackgroundService(ILogger<GameLoopService> logger) : IHostedService, IDisposable
{
    private readonly ILogger<GameLoopService> _logger = logger;
    //private Timer? _timer;
    private Task? _executingTask;
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly GameState _gameState = new();
    private const int TickRateMilliseconds = 100; // Roughly 10 FPS/TPS
    // A tick rate of 10 would mean 10 updates each second.

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
            // The gamestate should update every tick
            _gameState.UpdateState();

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
