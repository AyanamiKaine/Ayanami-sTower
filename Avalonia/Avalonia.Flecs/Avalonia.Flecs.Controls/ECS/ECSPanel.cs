using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Panel component
    /// </summary>
    public class ECSPanel : IFlecsModule
    {
        /// <summary>
        /// Initializes the Panel component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSPanel>();
            world
                .Component<Panel>("Panel")
                .OnSet(
                    (Entity e, ref Panel panel) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(panel);
                        }
                        else if (e.Get<object>().GetType() == typeof(Panel))
                        {
                            e.Set<object>(panel);
                        }
                        e.Set<Control>(panel);

                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<Panel>())
                        {
                            //We dont want to add the control twice,
                            //This otherwise throws an exception
                            if (parent.Get<Panel>().Children.Contains(panel))
                            {
                                return;
                            }
                            parent.Get<Panel>().Children.Add(panel);
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            parent.Get<ContentControl>().Content = panel;
                        }
                        else if (parent.Has<Viewbox>())
                        {
                            parent.Get<Viewbox>().Child = panel;
                        }
                        else if (parent.Has<Border>())
                        {
                            parent.Get<Border>().Child = panel;
                        }
                    }
                )
                .OnRemove(
                    (Entity e, ref Panel panel) =>
                    {
                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<Panel>())
                        {
                            parent.Get<Panel>().Children.Remove(panel);
                        }
                        if (parent.Has<ContentControl>())
                        {
                            parent.Get<ContentControl>().Content = null;
                        }
                    }
                );
        }
    }
}
