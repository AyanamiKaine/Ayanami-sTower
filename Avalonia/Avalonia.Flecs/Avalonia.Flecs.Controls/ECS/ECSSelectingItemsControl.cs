using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the SelectingItemsControl component
    /// </summary>
    public class ECSSelectingItemsControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the SelectingItemsControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSSelectingItemsControl>();
            world
                .Component<SelectingItemsControl>("SelectingItemsControl")
                .OnSet(
                    (Entity e, ref SelectingItemsControl selectingItemsControl) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(selectingItemsControl);
                        }
                        else if (e.Get<object>().GetType() == typeof(SelectingItemsControl))
                        {
                            e.Set<object>(selectingItemsControl);
                        }
                        e.Set<ItemsControl>(selectingItemsControl);
                    }
                )
                .OnRemove(
                    (Entity e, ref SelectingItemsControl selectingItemsControl) =>
                        e.Remove<ItemsControl>()
                );
        }
    }
}
