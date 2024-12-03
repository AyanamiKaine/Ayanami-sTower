using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSControl>();
            world.Component<Control>("Control")
                .OnSet((Entity e, ref Control control) =>
                {
                    if (!e.Has<object>())
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

                }).OnRemove((Entity e, ref Control control) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
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
                });
        }
    }
}
