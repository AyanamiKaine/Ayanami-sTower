using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the RadioButton component
    /// </summary>
    public class ECSRadioButton : IFlecsModule
    {
        /// <summary>
        /// Initializes the RadioButton component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSRadioButton>();

            world.Component<RadioButton>("RadioButton")
                .OnSet((Entity e, ref RadioButton radioButton) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(radioButton);
                    }

                    // We set the contentControl component so systems and queries in general can more easily
                    // access the generic .content property of the button.
                    // This is good so queries can be more generic and not have to check for every possible control type.
                    e.Set<ToggleButton>(radioButton);
                })
                .OnRemove((Entity e, ref RadioButton radioButton) =>
                {
                    e.Remove<ToggleButton>();
                });
        }
    }
}
