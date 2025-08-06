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
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public string Author => "Ayanami Kaine";

    /// <inheritdoc/>
    public string Description => "Provides core features for the ECS framework.";

    /// <inheritdoc/>
    public void Initialize(World world)
    {
        world.RegisterSystem(new MovementSystem2D());
        world.RegisterSystem(new MovementSystem3D());
    }

    /// <inheritdoc/>
    public void Uninitialize(World world)
    {
        world.RemoveSystem<MovementSystem2D>();
        world.RemoveSystem<MovementSystem3D>();
    }
}
