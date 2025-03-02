using Flecs.NET.Core;
using NLog;
using StellaInvicta.Systems;

namespace StellaInvicta;


/// <summary>
/// Provides extension methods for the ECS World class to handle component and tag registration.
/// </summary>
/// <remarks>
/// This static class contains helper methods to register components and tags within an ECS World instance.
/// It provides logging capabilities to track component and tag registration operations.
/// </remarks>
public static class ECSWorldExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Registers a tag component within the specified world.
    /// </summary>
    /// <typeparam name="T">The type of the tag component to register.</typeparam>
    /// <param name="world">The game world to which the tag will be added.</param>
    /// <param name="tagName">The name of the tag component.</param>
    public static Component<T> RegisterTag<T>(this World world, string tagName)
    {
        Logger.ConditionalDebug($"Registering {tagName} component tag.");
        return world.Component<T>(tagName);
    }

    /// <summary>
    /// Registers a component type in the specified ECS world with a given name.
    /// </summary>
    /// <typeparam name="T">The type of component to register.</typeparam>
    /// <param name="world">The ECS world instance where the component will be registered.</param>
    /// <param name="componentName">The name to associate with the component type.</param>
    /// <remarks>
    /// This method logs a debug message when registering the component.
    /// </remarks>
    public static Component<T> RegisterComponent<T>(this World world, string componentName)
    {
        Logger.ConditionalDebug($"Registering {componentName} as component.");
        return world.Component<T>(componentName);
    }

    /// <summary>
    /// Adds a system to the specified world and enables it.
    /// </summary>
    /// <param name="world">The world to which the system will be added.</param>
    /// <param name="system">The system to be added and enabled.</param>
    /// <param name="simulationSpeed">The timer entity that controls the tick rate of this system.</param>
    /// <returns>The entity representing the enabled system in the world.</returns>
    public static Entity AddSystem(this World world, ISystem system, TimerEntity simulationSpeed)
    {
        var systemEntity = system.Enable(world, simulationSpeed);
        Logger.ConditionalDebug($"Adding System with the name: '{systemEntity.Name()}'");

        return systemEntity;
    }
}