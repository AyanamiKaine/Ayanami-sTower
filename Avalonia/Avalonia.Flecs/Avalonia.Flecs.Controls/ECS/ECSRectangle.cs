using Avalonia.Controls.Shapes;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Rectangle component
    /// </summary>
    public class ECSRectangle : IFlecsModule
    {
        /// <summary>
        /// Initializes the Rectangle component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSRectangle>();
            world
                .Component<Rectangle>("Rectangle")
                .OnSet(
                    (Entity e, ref Rectangle rectangle) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(rectangle);
                        }
                        else if (e.Get<object>().GetType() == typeof(Rectangle))
                        {
                            e.Set<object>(rectangle);
                        }
                        e.Set<Shape>(rectangle);
                    }
                )
                .OnRemove((Entity e, ref Rectangle shape) => e.Remove<Shape>());
        }
    }
}
