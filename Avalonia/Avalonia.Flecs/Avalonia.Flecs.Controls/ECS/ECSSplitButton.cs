using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the SplitButton component
    /// </summary>
    public class ECSSplitButton : IFlecsModule
    {
        /// <summary>
        /// Initializes the SplitButton component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSSplitButton>();

            world.Component<SplitButton>("SplitButton")
                .OnSet((Entity e, ref SplitButton splitButton) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(splitButton);
                    }
                    e.Set<ContentControl>(splitButton);
                })
                .OnRemove((Entity e, ref SplitButton splitButton) =>
                {
                    e.Remove<ContentControl>();
                });
        }
    }
}
