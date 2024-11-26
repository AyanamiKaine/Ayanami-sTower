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
    public class ECSContentControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSContentControl>();
            world.Component<ContentControl>("ContentControl")
                .OnSet((Entity e, ref ContentControl contentControl) =>
                {
                    e.Set<TemplatedControl>(contentControl);
                }).OnRemove((Entity e, ref ContentControl contentControl) =>
                {
                    e.Remove<TemplatedControl>();
                });
        }
    }
}
