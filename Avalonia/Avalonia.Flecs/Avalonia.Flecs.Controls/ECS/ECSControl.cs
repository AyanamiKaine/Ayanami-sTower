using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Control component
    /// </summary>
    public class ECSControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the Control component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSControl>();
            world
                .Component<Control>("Control")
                .OnSet(
                    (Entity e, ref Control control) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(control);
                        }
                        else if (e.Get<object>().GetType() == typeof(Control))
                        {
                            e.Set<object>(control);
                        }

                        e.Set<InputElement>(control);

                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<Panel>())
                        {
                            if (parent.Get<Panel>().Children.Contains(control))
                            {
                                return;
                            }

                            parent.Get<Panel>().Children.Add(control);
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            parent.Get<ContentControl>().Content = control;
                        }
                    }
                )
                .OnRemove(
                    (Entity e, ref Control control) =>
                    {
                        /*
                        We are mostly settings some properties null
                        so we dont acidentially refrence objects
                        that then wont get collected.
                        */
                        control.FocusAdorner = null;
                        control.Cursor = null;
                        control.Tag = null;
                        control.ContextFlyout = null;
                        control.ContextMenu = null;

                        var parent = e.Parent();
                        if (parent.InValid())
                        {
                            return;
                        }
                        if (parent.Has<Panel>())
                        {
                            parent.Get<Panel>().Children.Remove(control);
                        }
                        if (parent.Has<ContentControl>())
                        {
                            parent.Get<ContentControl>().Content = null;
                        }
                        Dispatcher.UIThread.Invoke(() => e.Remove<InputElement>());
                    }
                );
        }
    }
}
