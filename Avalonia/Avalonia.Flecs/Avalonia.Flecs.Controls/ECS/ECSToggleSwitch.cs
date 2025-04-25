using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ToggleSwitch component
    /// </summary>
    public class ECSToggleSwitch : IFlecsModule
    {
        /// <summary>
        /// Initializes the ToggleSwitch component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSToggleSwitch>();

            world
                .Component<ToggleSwitch>("ToggleSwitch")
                .OnSet(
                    (Entity e, ref ToggleSwitch toggleSwitch) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(toggleSwitch);
                        }
                        else if (e.Get<object>().GetType() == typeof(ToggleSwitch))
                        {
                            e.Set<object>(toggleSwitch);
                        }
                        // We set the contentControl component so systems and queries in general can more easily
                        // access the generic .content property of the button.
                        // This is good so queries can be more generic and not have to check for every possible control type.
                        e.Set<ToggleButton>(toggleSwitch);
                    }
                )
                .OnRemove((Entity e, ref ToggleSwitch toggleSwitch) => e.Remove<ToggleButton>());
        }
    }
}
