using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSTemplatedControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSTemplatedControl>();
            world.Component<TemplatedControl>("TemplatedControl")
                .OnSet((Entity e, ref TemplatedControl templatedControl) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(templatedControl);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = templatedControl;
                    }
                })
                .OnRemove((Entity e, ref TemplatedControl templatedControl) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(templatedControl);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });
        }
    }
}
