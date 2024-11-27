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
    public class ECSListBox : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSListBox>();
            world.Component<ListBox>("ListBox")
                .OnSet((Entity e, ref ListBox listBox) =>
                {
                    e.Set<SelectingItemsControl>(listBox);
                })
                .OnRemove((Entity e, ref ListBox listBox) =>
                {
                    e.Remove<SelectingItemsControl>();
                });
        }
    }
}