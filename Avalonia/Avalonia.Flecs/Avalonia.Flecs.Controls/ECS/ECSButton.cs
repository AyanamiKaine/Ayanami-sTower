using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Button component
    /// </summary>
    public class ECSButton : IFlecsModule
    {
        /// <summary>
        /// Initializes the Button component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSButton>();

            world.Component<Button>("Button")
                .OnSet((Entity e, ref Button button) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(button);
                    }

                    // We set the contentControl component so systems and queries in general can more easily
                    // access the generic .content property of the button.
                    // This is good so queries can be more generic and not have to check for every possible control type.
                    e.Set<ContentControl>(button);
                })
                .OnRemove((Entity e, ref Button _) => e.Remove<ContentControl>());
        }
    }
}
