using Flecs.NET.Core;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Represents the ECS module for Avalonia Fluent UI controls, responsible for importing necessary components.
    /// </summary>
    // Modules need to implement the IFlecsModule interface
    public struct Module : IFlecsModule
    {
        /// <summary>
        /// Initializes the module by registering it and importing other ECS components into the world.
        /// </summary>
        /// <param name="world">The Flecs world to initialize the module in.</param>
        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();
            world.Import<ECSNavigationView>();
            world.Import<ECSNavigationViewItem>();
            world.Import<ECSNavigationViewItemHeader>();
            world.Import<ECSSettingsExpander>();
            world.Import<ECSFrame>();
        }
    }
}
