using System;

namespace AyanamisTower.StellaEcs.Systems;

/// <summary>
/// Defines some core systems
/// </summary>
public class CorePlugin : IPlugin
{
    /// <inheritdoc/>
    public string Name => "Core Features";

    /// <inheritdoc/>
    public void Initialize(World world)
    {
        world.RegisterSystem(new MovementSystem2D());
        world.RegisterSystem(new MovementSystem3D());
    }
}
