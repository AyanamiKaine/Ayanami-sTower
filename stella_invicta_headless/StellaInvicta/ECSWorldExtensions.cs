using Flecs.NET.Core;
using NLog;

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
    public static void RegisterTag<T>(this World world, string tagName)
    {
        Logger.ConditionalDebug($"Registering {tagName} component tag.");
        world.Component<T>(tagName);
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
    public static void RegisterComponent<T>(this World world, string componentName)
    {
        Logger.ConditionalDebug($"Registering {componentName} as component.");
        world.Component<T>(componentName);
    }
}