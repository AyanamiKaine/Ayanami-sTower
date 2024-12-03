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
    public class ECSDecorator : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSBorder>();

            world.Component<Decorator>("Decorator")
                .OnSet((Entity e, ref Decorator decorator) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(decorator);
                    }

                    e.Set<Control>(decorator);
                })
                .OnRemove((Entity e, ref Decorator decorator) =>
                {
                    e.Remove<Control>();
                });
        }
    }
}
