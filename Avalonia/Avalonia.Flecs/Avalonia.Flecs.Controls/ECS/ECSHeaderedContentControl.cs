using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the HeaderedContentControl component
    /// </summary>
    public class ECSHeaderedContentControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the HeaderedContentControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSHeaderedContentControl>();
            world
                .Component<HeaderedContentControl>("HeaderedContentControl")
                .OnSet(
                    (Entity e, ref HeaderedContentControl headeredContentControl) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(headeredContentControl);
                        }
                        else if (e.Get<object>().GetType() == typeof(HeaderedContentControl))
                        {
                            e.Set<object>(headeredContentControl);
                        }
                        e.Set<ContentControl>(headeredContentControl);
                    }
                )
                .OnRemove((Entity e, ref HeaderedContentControl _) => e.Remove<ContentControl>());
        }
    }
}
