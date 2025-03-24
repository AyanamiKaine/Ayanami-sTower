using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Viewbox component
    /// </summary>
    public class ECSViewbox : IFlecsModule
    {
        /// <summary>
        /// Initializes the Viewbox component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSViewbox>();
            world.Component<Viewbox>("Viewbox")
                .OnSet((Entity e, ref Viewbox viewbox) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(viewbox);
                    }
                    else if (e.Get<object>().GetType() == typeof(Viewbox))
                    {
                        e.Set<object>(viewbox);
                    }
                    e.Set<Control>(viewbox);
                })
                .OnRemove((Entity e, ref Viewbox viewbox) => e.Remove<Control>());
        }
    }
}
