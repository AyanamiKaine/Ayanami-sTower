using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Input.TextInput;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSTextBlock : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSTextBlock>();
            world.Component<TextBlock>("TextBlock")
                                        .OnSet((Entity e, ref TextBlock textBlock) =>
                                        {
                                            if (!e.Has<object>())
                                            {
                                                e.Set<object>(textBlock);
                                            }
                                            e.Set<Control>(textBlock);

                                        })
                            .OnRemove((Entity e, ref TextBlock textBlock) =>
                            {
                                e.Remove<Control>();
                            }); ;
        }
    }
}
