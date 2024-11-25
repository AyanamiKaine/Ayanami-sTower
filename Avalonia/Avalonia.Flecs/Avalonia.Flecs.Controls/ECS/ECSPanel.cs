using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSPanel : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSPanel>();
            world.Component<Panel>("Panel")
                .OnSet((Entity e, ref Panel panel) =>
                {
                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<Panel>())
                        {
                            parent.Get<Panel>().Children.Add(panel);
                        }
                        else if (parent.Has<ContentControl>())
                        {
                            parent.Get<ContentControl>().Content = panel;
                        }

                }).OnRemove((Entity e, ref Panel panel) =>
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
                });
        }
    }
}
