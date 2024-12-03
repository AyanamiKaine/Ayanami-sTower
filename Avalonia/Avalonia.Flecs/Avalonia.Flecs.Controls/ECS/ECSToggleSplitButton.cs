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
    public class ECSToggleSplitButton : IFlecsModule
    {
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
