using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the HeaderedSelectingItemsControl component
    /// </summary>
    public class ECSHeaderedSelectingItemsControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the HeaderedSelectingItemsControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSHeaderedSelectingItemsControl>();
            world.Component<HeaderedSelectingItemsControl>("HeaderedSelectingItemsControl")
                .OnSet((Entity e, ref HeaderedSelectingItemsControl headeredSelectingItemsControl) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(headeredSelectingItemsControl);
                    }
                    e.Set<SelectingItemsControl>(headeredSelectingItemsControl);

                }).OnRemove((Entity e, ref HeaderedSelectingItemsControl headeredSelectingItemsControl) =>
                {
                    e.Remove<SelectingItemsControl>();
                });
        }
    }
}
