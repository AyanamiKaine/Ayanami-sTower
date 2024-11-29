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
    public class ECSFlyoutBase : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSFlyoutBase>();
            world.Component<FlyoutBase>("FlyoutBase")
                .OnSet((Entity e, ref FlyoutBase flyoutBase) =>
                {
                    e.Set<AvaloniaObject>(flyoutBase);

                    flyoutBase.Closed += (object? sender, EventArgs args) =>
                    {
                        e.Set(new Closed(sender, args));
                        e.Emit<Closed>();
                    };

                    flyoutBase.Opened += (object? sender, EventArgs args) =>
                    {
                        e.Set(new Opened(sender, args));
                        e.Emit<Opened>();
                    };

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