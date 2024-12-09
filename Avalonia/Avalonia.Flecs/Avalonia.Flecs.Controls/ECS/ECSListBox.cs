using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ListBox component
    /// </summary>
    public class ECSListBox : IFlecsModule
    {
        /// <summary>
        /// Initializes the ListBox component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSListBox>();
            world.Component<ListBox>("ListBox")
                .OnSet((Entity e, ref ListBox listBox) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(listBox);
                    }
                    e.Set<SelectingItemsControl>(listBox);
                })
                .OnRemove((Entity e, ref ListBox listBox) => e.Remove<SelectingItemsControl>());
        }
    }
}