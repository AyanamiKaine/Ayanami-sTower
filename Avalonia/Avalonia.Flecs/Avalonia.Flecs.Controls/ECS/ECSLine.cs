using Avalonia.Controls.Shapes;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Line component
    /// </summary>
    public class ECSLine : IFlecsModule
    {
        /// <summary>
        /// Initializes the Ellipse component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSLine>();
            world
                .Component<Line>("Line")
                .OnSet(
                    (Entity e, ref Line line) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(line);
                        }
                        else if (e.Get<object>().GetType() == typeof(Line))
                        {
                            e.Set<object>(line);
                        }
                        e.Set<Shape>(line);
                    }
                )
                .OnRemove((Entity e, ref Line line) => e.Remove<Shape>());
        }
    }
}
