using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Canvas component
    /// </summary>
    public class ECSCanvas : IFlecsModule
    {
        /// <summary>
        /// Initializes the Canvas component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSCanvas>();
            world.Component<Canvas>("Canvas")
                .OnSet((Entity e, ref Canvas canvas) =>
                {
                    if (!e.Has<object>())
                    {
                        e.Set<object>(canvas);
                    }
                    else if (e.Get<object>().GetType() == typeof(Canvas))
                    {
                        e.Set<object>(canvas);
                    }
                    e.Set<Panel>(canvas);
                }).OnRemove((Entity e, ref Canvas _) => e.Remove<Panel>());
        }
    }
}
