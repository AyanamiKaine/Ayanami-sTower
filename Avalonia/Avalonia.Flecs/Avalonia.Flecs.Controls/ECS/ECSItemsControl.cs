using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ItemsControl component
    /// </summary>
    public class ECSItemsControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the ItemsControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSItemsControl>();
            world
                .Component<ItemsControl>("ItemsControl")
                .OnSet(
                    (Entity e, ref ItemsControl itemsControl) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(itemsControl);
                        }
                        else if (e.Get<object>().GetType() == typeof(ItemsControl))
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
                    }
                )
                .OnRemove(
                    (Entity e, ref ItemsControl itemsControl) =>
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
                    }
                );
        }
    }
}
