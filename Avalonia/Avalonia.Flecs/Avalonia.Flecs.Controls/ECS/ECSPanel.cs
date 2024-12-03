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
                    if (!e.Has<object>())
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
