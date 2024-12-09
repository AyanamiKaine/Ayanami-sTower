using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Expander component
    /// </summary>
    public class ECSExpander : IFlecsModule
    {
        /// <summary>
        /// Initializes the Expander component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSExpander>();
            world.Component<Expander>("Expander")
                .OnSet((Entity e, ref Expander expander) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(expander);
                    }
                    e.Set<HeaderedContentControl>(expander);
                }).OnRemove((Entity e, ref Expander expander) =>
                {
                    e.Remove<HeaderedContentControl>();
                });
        }
    }
}
