using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the MenuFlyout component
    /// </summary>
    public class ECSMenuFlyout : IFlecsModule
    {
        /// <summary>
        /// Initializes the MenuFlyout component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSMenuFlyout>();
            world.Component<MenuFlyout>("MenuFlyout")
                .OnSet((Entity e, ref MenuFlyout menuFlyout) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(menuFlyout);
                    }
                    e.Set<PopupFlyoutBase>(menuFlyout);
                })
                .OnRemove((Entity e, ref MenuFlyout menuFlyout) =>
                {
                    e.Remove<PopupFlyoutBase>();
                });
        }
    }
}