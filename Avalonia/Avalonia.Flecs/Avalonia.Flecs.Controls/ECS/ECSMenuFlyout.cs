using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input.TextInput;
using Avalonia.Flecs.Controls.ECS.Events;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSMenuFlyout : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSMenuFlyout>();
            world.Component<MenuFlyout>("MenuFlyout")
                .OnSet((Entity e, ref MenuFlyout menuFlyout) =>
                {
                    e.Set<PopupFlyoutBase>(menuFlyout);
                })
                .OnRemove((Entity e, ref MenuFlyout menuFlyout) =>
                {
                    e.Remove<PopupFlyoutBase>();
                });
        }
    }
}