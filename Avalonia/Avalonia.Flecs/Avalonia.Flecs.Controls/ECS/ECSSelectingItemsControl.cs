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
                    if (!e.Has<object>())
                    {
                        e.Set<object>(selectingItemsControl);
                    }
                    e.Set<ItemsControl>(selectingItemsControl);
                })
                .OnRemove((Entity e, ref SelectingItemsControl selectingItemsControl) =>
                {
                    e.Remove<ItemsControl>();
                });
        }
    }
}