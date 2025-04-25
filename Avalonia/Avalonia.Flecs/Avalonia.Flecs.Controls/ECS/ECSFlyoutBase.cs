using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;

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
            world
                .Component<FlyoutBase>("FlyoutBase")
                .OnSet(
                    (Entity e, ref FlyoutBase flyoutBase) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(flyoutBase);
                        }
                        else if (e.Get<object>().GetType() == typeof(FlyoutBase))
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
                    }
                )
                .OnRemove(
                    (Entity e, ref FlyoutBase _) =>
                    {
                        e.Remove<AvaloniaObject>();

                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<Control>())
                        {
                            parent.Get<Control>().ContextFlyout = null;
                        }
                    }
                );
        }
    }
}
