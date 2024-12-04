using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSMenu : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSMenu>();
            world.Component<Menu>("Menu")
                            .OnSet((Entity e, ref Menu menu) =>
                            {
                                if (!e.Has<object>())
                                {
                                    e.Set<object>(menu);
                                }
                                
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<ContentControl>())
                                {
                                    parent.Get<ContentControl>().Content = menu;
                                }

                                if (parent.Has<Panel>())
                                {
                                    parent.Get<Panel>().Children.Add(menu);
                                }

                                DockPanel.SetDock(menu, Dock.Top);

                            }).OnRemove((Entity e, ref Menu menu) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<ContentControl>())
                                {
                                    parent.Get<ContentControl>().Content = null;
                                }

                                if (parent.Has<Panel>())
                                {
                                    parent.Get<Panel>().Children.Remove(menu);
                                }
                            });
        }
    }
}
