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
    public class ECSSelectingItemsControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSSelectingItemsControl>();
            world.Component<SelectingItemsControl>("SelectingItemsControl")
                .OnSet((Entity e, ref SelectingItemsControl selectingItemsControl) =>
                {
                    e.Set<ItemsControl>(selectingItemsControl);

                    selectingItemsControl.SelectionChanged += (object sender, SelectionChangedEventArgs args) =>
                    {
                        e.Set(new SelectionChanged(sender, args));
                        e.Emit<SelectionChanged>();
                    };
                })
                .OnRemove((Entity e, ref SelectingItemsControl selectingItemsControl) =>
                {
                    e.Remove<ItemsControl>();
                });
        }
    }
}