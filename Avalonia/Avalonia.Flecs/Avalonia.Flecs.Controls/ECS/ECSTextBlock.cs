using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the TextBlock component
    /// </summary>
    public class ECSTextBlock : IFlecsModule
    {
        /// <summary>
        /// Initializes the TextBlock component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSTextBlock>();
            world.Component<TextBlock>("TextBlock")
                                        .OnSet((Entity e, ref TextBlock textBlock) =>
                                        {
                                            if (!e.Has<object>())
                                            {
                                                e.Set<object>(textBlock);
                                            }
                                            e.Set<Control>(textBlock);
                                        })
                            .OnRemove((Entity e, ref TextBlock textBlock) => e.Remove<Control>());
            ;
        }
    }
}
