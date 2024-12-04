using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Input.TextInput;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSWindow : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSWindow>();
            world.Component<Window>("Window")
                           .OnSet((Entity e, ref Window window) =>
                           {
                               if (!e.Has<object>())
                               {
                                   e.Set<object>(window);
                               }
                               e.Set<ContentControl>(window);
                           })
                           .OnRemove((Entity e, ref Window window) =>
                           {
                               window.Close();
                           });
        }
    }
}
