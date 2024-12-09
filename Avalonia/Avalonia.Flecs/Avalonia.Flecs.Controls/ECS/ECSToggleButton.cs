using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ToggleButton component
    /// </summary>
    public class ECSToggleButton : IFlecsModule
    {
        /// <summary>
        /// Initializes the ToggleButton component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSToggleButton>();

            world.Component<ToggleButton>("ToggleButton")
                .OnSet((Entity e, ref ToggleButton toggleButton) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(toggleButton);
                    }

                    // We set the contentControl component so systems and queries in general can more easily
                    // access the generic .content property of the button.
                    // This is good so queries can be more generic and not have to check for every possible control type.
                    e.Set<Button>(toggleButton);
                })
                .OnRemove((Entity e, ref ToggleButton toggleButton) => e.Remove<Button>());
        }
    }
}
