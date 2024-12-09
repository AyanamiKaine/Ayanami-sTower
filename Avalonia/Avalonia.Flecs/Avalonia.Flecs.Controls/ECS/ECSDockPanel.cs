using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the DockPanel component
    /// </summary>
    public class ECSDockPanel : IFlecsModule
    {
        /// <summary>
        /// Initializes the DockPanel component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSDockPanel>();
            world.Component<DockPanel>("DockPanel")
                .OnSet((Entity e, ref DockPanel dockPanel) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(dockPanel);
                    }
                    // We set the panel component so systems and queries in general can more easily
                    // access the generic .children.add property of the panel.
                    // This is good so queries can be more generic and not have to check for every possible panel type.
                    e.Set<Panel>(dockPanel);
                }).OnRemove((Entity e, ref DockPanel dockPanel) =>
                {
                    e.Remove<Panel>();
                });
        }
    }
}
