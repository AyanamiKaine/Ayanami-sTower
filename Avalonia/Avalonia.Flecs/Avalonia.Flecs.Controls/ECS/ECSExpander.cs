using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSExpander : IFlecsModule
    {
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
