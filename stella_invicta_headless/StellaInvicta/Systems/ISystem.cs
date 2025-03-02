using Flecs.NET.Core;

namespace StellaInvicta.Systems;

/// <summary>
/// Represents a system interface for the ECS (Entity Component System) architecture.
/// Systems are responsible for processing entities and their components.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Enables the system and if its not already part of the ecs world adds it automatically.
    /// </summary>
    /// <param name="world">The ECS world to enable the system in.</param>
    /// <param name="simulationSpeed">The simulation speed timer entity. It determines how fast the system runs</param>
    public Entity Enable(World world, TimerEntity simulationSpeed);

    /// <summary>
    /// Disables the systems
    /// </summary>
    public void Disable();
}