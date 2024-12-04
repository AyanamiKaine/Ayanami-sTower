using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSTextBox : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSTextBox>();
            world.Component<TextBox>("TextBox")
                            .OnSet((Entity e, ref TextBox textBox) =>
                            {
                                if (!e.Has<object>())
                                {
                                    e.Set<object>(textBox);
                                }
                                e.Set<TemplatedControl>(textBox);

                                // Adding event handlers
                                // https://reference.avaloniaui.net/api/Avalonia.Controls.TextBox/#Events

                                
                            }).OnRemove((Entity e, ref TextBox textBox) =>
                            {
                                e.Remove<TemplatedControl>();
                            });
        }
    }
}
