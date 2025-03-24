using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Shape component
    /// </summary>
    public class ECSShape : IFlecsModule
    {
        /// <summary>
        /// Initializes the Shape component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSShape>();
            world.Component<Shape>("Shape")
                .OnSet((Entity e, ref Shape shape) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(shape);
                    }
                    else if (e.Get<object>().GetType() == typeof(Shape))
                    {
                        e.Set<object>(shape);
                    }
                    e.Set<Control>(shape);
                })
                .OnRemove((Entity e, ref Shape shape) => e.Remove<Control>());
        }
    }
}
