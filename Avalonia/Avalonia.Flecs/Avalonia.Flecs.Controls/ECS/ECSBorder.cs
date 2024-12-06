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
    /// This ECS Module is used to register the Border component
    /// </summary>
    public class ECSBorder : IFlecsModule
    {
        /// <summary>
        /// Initializes the Border component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSBorder>();

            world.Component<Border>("Border")
                .OnSet((Entity e, ref Border border) =>
                {
                    if(!e.Has<object>())
                    {
                        e.Set<object>(border);
                    }

                    e.Set<Decorator>(border);
                })
                .OnRemove((Entity e, ref Border border) =>
                {
                    e.Remove<Decorator>();
                });
        }
    }
}
