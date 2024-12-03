using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSHeaderedContentControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSHeaderedContentControl>();
            world.Component<HeaderedContentControl>("HeaderedContentControl")
                .OnSet((Entity e, ref HeaderedContentControl headeredContentControl) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(headeredContentControl);
                    }
                    e.Set<ContentControl>(headeredContentControl);

                }).OnRemove((Entity e, ref HeaderedContentControl headeredContentControl) =>
                {
                    e.Remove<ContentControl>();
                });
        }
    }
}
