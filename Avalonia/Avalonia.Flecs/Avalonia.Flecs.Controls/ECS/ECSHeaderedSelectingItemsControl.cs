using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSHeaderedSelectingItemsControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSHeaderedSelectingItemsControl>();
            world.Component<HeaderedSelectingItemsControl>("HeaderedSelectingItemsControl")
                .OnSet((Entity e, ref HeaderedSelectingItemsControl headeredContentControl) =>
                {
                    e.Set<SelectingItemsControl>(headeredContentControl);

                }).OnRemove((Entity e, ref HeaderedSelectingItemsControl headeredContentControl) =>
                {
                    e.Remove<SelectingItemsControl>();
                });
        }
    }
}
