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
    /// <summary>
    /// This ECS Module is used to register the Window component
    /// </summary>
    public class ECSWindow : IFlecsModule
    {
        /// <summary>
        /// Initializes the Window component
        /// </summary>
        /// <param name="world"></param>
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
