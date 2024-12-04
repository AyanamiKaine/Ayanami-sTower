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
    public class ECSSplitButton : IFlecsModule
    {
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
