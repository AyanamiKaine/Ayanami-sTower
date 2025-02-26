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
    /// <param name="world"></param>
    public Entity Enable(World world);

    /// <summary>
    /// Disables the systems
    /// </summary>
    public void Disable();
}