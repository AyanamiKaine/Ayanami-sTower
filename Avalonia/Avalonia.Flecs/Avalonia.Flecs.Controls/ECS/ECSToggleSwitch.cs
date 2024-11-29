using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input.TextInput;
using Avalonia.Flecs.Controls.ECS.Events;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSToggleSwitch : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSToggleSwitch>();

            world.Component<ToggleSwitch>("ToggleSwitch")
                .OnSet((Entity e, ref ToggleSwitch toggleSwitch) =>
                {
                    // We set the contentControl component so systems and queries in general can more easily
                    // access the generic .content property of the button.
                    // This is good so queries can be more generic and not have to check for every possible control type.
                    e.Set<ToggleButton>(toggleSwitch);
                })
                .OnRemove((Entity e, ref ToggleSwitch toggleSwitch) =>
                {
                    e.Remove<ToggleButton>();
                });
        }
    }
}
