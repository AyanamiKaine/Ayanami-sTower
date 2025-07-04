using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Decorator component
    /// </summary>
    public class ECSDecorator : IFlecsModule
    {
        /// <summary>
        /// Initializes the Decorator component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSDecorator>();

            world
                .Component<Decorator>("Decorator")
                .OnSet(
                    (Entity e, ref Decorator decorator) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(decorator);
                        }
                        else if (e.Get<object>().GetType() == typeof(Decorator))
                        {
                            e.Set<object>(decorator);
                        }

                        e.Set<Control>(decorator);
                    }
                )
                .OnRemove((Entity e, ref Decorator _) => e.Remove<Control>());
        }
    }
}
