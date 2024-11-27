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

                    e.Set<ContentControl>(splitButton);

                    splitButton.Click += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new Click(sender, args));
                        e.Emit<Click>();
                    };

                })
                .OnRemove((Entity e, ref SplitButton splitButton) =>
                {
                    e.Remove<ContentControl>();
                });
        }
    }
}
