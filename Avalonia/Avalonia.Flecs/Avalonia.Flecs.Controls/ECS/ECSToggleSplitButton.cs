using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ToggleSplitButton component
    /// </summary>
    public class ECSToggleSplitButton : IFlecsModule
    {
        /// <summary>
        /// Initializes the ToggleSplitButton component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSToggleSplitButton>();

            world.Component<ToggleSplitButton>("ToggleSplitButton")
                .OnSet((Entity e, ref ToggleSplitButton toggleSplitButton) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(toggleSplitButton);
                    }
                    e.Set<SplitButton>(toggleSplitButton);
                })
                .OnRemove((Entity e, ref ToggleSplitButton toggleSplitButton) =>
                {
                    e.Remove<SplitButton>();
                });
        }
    }
}
