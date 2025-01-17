using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the SelectingItemsControl component
    /// </summary>
    public class ECSSeparator : IFlecsModule
    {
        /// <summary>
        /// Initializes the SelectingItemsControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSSeparator>();
            world.Component<Separator>("Separator")
                .OnSet((Entity e, ref Separator separator) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(separator);
                    }
                    else if (e.Get<object>().GetType() == typeof(Separator))
                    {
                        e.Set<object>(separator);
                    }
                    e.Set<TemplatedControl>(separator);
                })
                .OnRemove((Entity e, ref Separator separator) => e.Remove<Separator>());
        }
    }
}