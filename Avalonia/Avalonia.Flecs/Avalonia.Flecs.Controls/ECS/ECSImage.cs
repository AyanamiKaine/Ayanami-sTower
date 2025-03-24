using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Image component
    /// </summary>
    public class ECSImage : IFlecsModule
    {
        /// <summary>
        /// Initializes the Image component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSImage>();
            world.Component<Image>("Image")
                .OnSet((Entity e, ref Image image) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(image);
                    }
                    else if (e.Get<object>().GetType() == typeof(Image))
                    {
                        e.Set<object>(image);
                    }
                    e.Set<Control>(image);
                })
                .OnRemove((Entity e, ref Image image) => e.Remove<Control>());
        }
    }
}
