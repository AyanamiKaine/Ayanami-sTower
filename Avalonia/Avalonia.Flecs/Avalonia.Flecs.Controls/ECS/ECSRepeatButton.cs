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
    public class ECSRepeatButton : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSRepeatButton>();

            world.Component<RepeatButton>("RepeatButton")
                .OnSet((Entity e, ref RepeatButton repeatButton) =>
                {

                    // We set the contentControl component so systems and queries in general can more easily
                    // access the generic .content property of the button.
                    // This is good so queries can be more generic and not have to check for every possible control type.
                    e.Set<Button>(repeatButton);

                })
                .OnRemove((Entity e, ref RepeatButton repeatButton) =>
                {
                    e.Remove<Button>();
                });
        }
    }
}
