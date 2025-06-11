using Avalonia.Controls.Shapes;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Ellipse component
    /// </summary>
    public class ECSEllipse : IFlecsModule
    {
        /// <summary>
        /// Initializes the Ellipse component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSEllipse>();
            world
                .Component<Ellipse>("Ellipse")
                .OnSet(
                    (Entity e, ref Ellipse ellipse) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(ellipse);
                        }
                        else if (e.Get<object>().GetType() == typeof(Ellipse))
                        {
                            e.Set<object>(ellipse);
                        }
                        e.Set<Shape>(ellipse);
                    }
                )
                .OnRemove((Entity e, ref Ellipse shape) => e.Remove<Shape>());
        }
    }
}
