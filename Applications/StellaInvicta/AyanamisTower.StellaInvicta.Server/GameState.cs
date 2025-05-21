using System;
using Flecs.NET.Core;

namespace AyanamisTower.StellaInvicta.Server;

/// <summary>
/// Internal Gamestate
/// </summary>
public class GameState
{
    private readonly Lock _lock = new();
    private int _tickCount;
    private DateTime _lastUpdate;
    private readonly World _world = World.Create();

    /// <summary>
    /// Default
    /// </summary>
    public GameState()
    {
        _world.System().Each(() =>
        {
            _tickCount++;
            _lastUpdate = DateTime.UtcNow;
            // TODO: Add your actual game state update logic here
            // e.g., move players, update objects, check conditions
            Console.WriteLine($"Game Loop Tick: {_tickCount}, Last Update: {_lastUpdate}");
        });
    }

    /// <summary>
    /// EXAMPLE
    /// </summary>
    public void UpdateState()
    {
        lock (_lock)
        {
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