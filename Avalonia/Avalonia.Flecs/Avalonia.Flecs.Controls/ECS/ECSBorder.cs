using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Border component
    /// </summary>
    public class ECSBorder : IFlecsModule
    {
        /// <summary>
        /// Initializes the Border component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSBorder>();

            world.Component<Border>("Border")
                .OnSet((Entity e, ref Border border) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(border);
                    }
                    else if (e.Get<object>().GetType() == typeof(Border))
                    {
                        e.Set<object>(border);
                    }

                    e.Set<Decorator>(border);
                })
                .OnRemove((Entity e, ref Border _) => e.Remove<Decorator>());
        }
    }
}
