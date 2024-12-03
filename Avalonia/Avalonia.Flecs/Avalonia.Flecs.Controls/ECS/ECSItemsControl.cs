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
    public class ECSItemsControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSItemsControl>();
            world.Component<ItemsControl>("ItemsControl")
                .OnSet((Entity e, ref ItemsControl itemsControl) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(itemsControl);
                    }
                    e.Set<Control>(itemsControl);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        if (parent.Get<Panel>().Children.Contains(itemsControl))
                        {
                            return;
                        }
                        parent.Get<Panel>().Children.Add(itemsControl);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = itemsControl;
                    }
                })
                .OnRemove((Entity e, ref ItemsControl itemsControl) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(itemsControl);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });
        }
    }
}