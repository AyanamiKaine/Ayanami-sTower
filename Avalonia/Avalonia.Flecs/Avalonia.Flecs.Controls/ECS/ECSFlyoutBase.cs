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
    /// <summary>
    /// This ECS Module is used to register the FlyoutBase component
    /// </summary>
    public class ECSFlyoutBase : IFlecsModule
    {
        /// <summary>
        /// Initializes the FlyoutBase component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSFlyoutBase>();
            world.Component<FlyoutBase>("FlyoutBase")
                .OnSet((Entity e, ref FlyoutBase flyoutBase) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(flyoutBase);
                    }
                    e.Set<AvaloniaObject>(flyoutBase);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Control>())
                    {
                        parent.Get<Control>().ContextFlyout = flyoutBase;
                    }
                })
                .OnRemove((Entity e, ref FlyoutBase flyoutBase) =>
                {
                    e.Remove<AvaloniaObject>();
                });
        }
    }
}