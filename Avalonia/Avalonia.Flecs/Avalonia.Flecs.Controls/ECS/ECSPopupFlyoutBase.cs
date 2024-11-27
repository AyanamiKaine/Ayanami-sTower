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
    public class ECSPopupFlyoutBase : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSPopupFlyoutBase>();
            world.Component<PopupFlyoutBase>("MenuFlyout")
                .OnSet((Entity e, ref PopupFlyoutBase popupFlyoutBase) =>
                {
                    e.Set<FlyoutBase>(popupFlyoutBase);
                })
                .OnRemove((Entity e, ref PopupFlyoutBase popupFlyoutBase) =>
                {
                    e.Remove<FlyoutBase>();
                });
        }
    }
}