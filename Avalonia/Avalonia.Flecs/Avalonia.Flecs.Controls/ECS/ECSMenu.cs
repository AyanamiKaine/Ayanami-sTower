using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Menu component
    /// </summary>
    public class ECSMenu : IFlecsModule
    {
        /// <summary>
        /// Initializes the Menu component
        /// </summary>
        /// <param name="world"></param>
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
                                else if (e.Get<object>().GetType() == typeof(Menu))
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
