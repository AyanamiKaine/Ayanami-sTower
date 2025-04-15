using Flecs.NET.Core;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    // Modules need to implement the IFlecsModule interface
    public struct Module : IFlecsModule
    {
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