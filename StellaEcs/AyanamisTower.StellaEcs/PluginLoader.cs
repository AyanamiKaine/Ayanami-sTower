using System;
using System.Reflection;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// General plugin loader
/// </summary>
public static class PluginLoader
{
    /// <summary>
    /// Loads plugins into the ecs world
    /// </summary>
    /// <param name="world"></param>
    /// <param name="pluginPath"></param>
    public static void LoadPlugins(World world, string pluginPath = "Plugins")
    {
        if (!Directory.Exists(pluginPath)) return;

        foreach (var file in Directory.GetFiles(pluginPath, "*.dll"))
        {
            Assembly pluginAssembly = Assembly.LoadFrom(Path.GetFullPath(file));

            // Find all types in the DLL that implement our IPlugin interface
            foreach (var type in pluginAssembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface))
            {
                // Create an instance of the plugin's entry point class
                IPlugin? pluginInstance = (IPlugin)Activator.CreateInstance(type)!;
                Console.WriteLine($"--- Loading Plugin: {pluginInstance?.Name} ---");

                if (pluginInstance != null)
                {
                    // Register the plugin with the world for tracking
                    world.RegisterPlugin(pluginInstance);

                    // Call its Initialize method, passing in our world!
                    pluginInstance.Initialize(world);
                }
            }
        }
    }
}