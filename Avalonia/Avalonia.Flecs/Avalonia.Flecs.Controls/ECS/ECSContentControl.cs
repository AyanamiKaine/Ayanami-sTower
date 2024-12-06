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
    /// <summary>
    /// This ECS Module is used to register the ContentControl component
    /// </summary>
    public class ECSContentControl : IFlecsModule
    {
        /// <summary>
        /// Initializes the ContentControl component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSContentControl>();
            world.Component<ContentControl>("ContentControl")
                .OnSet((Entity e, ref ContentControl contentControl) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(contentControl);
                    }
                    e.Set<TemplatedControl>(contentControl);
                }).OnRemove((Entity e, ref ContentControl contentControl) =>
                {
                    e.Remove<TemplatedControl>();
                });
        }
    }
}
