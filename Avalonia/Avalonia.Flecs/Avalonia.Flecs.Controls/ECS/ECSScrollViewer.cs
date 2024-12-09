using Flecs.NET.Core;
using Avalonia.Controls;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ScrollViewer component
    /// </summary>
    public class ECSScrollViewer : IFlecsModule
    {
        /// <summary>
        /// Initializes the ScrollViewer component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSScrollViewer>();
            world.Component<ScrollViewer>("ScrollViewer")
                            .OnSet((Entity e, ref ScrollViewer scrollViewer) =>
                            {
                                if (!e.Has<object>())
                                {
                                    e.Set<object>(scrollViewer);
                                }
                                e.Set<ContentControl>(scrollViewer);
                                // Adding event handlers
                                // https://reference.avaloniaui.net/api/Avalonia.Controls.ScrollViewer/#Events

                            }).OnRemove((Entity e, ref ScrollViewer scrollViewer) =>
                            {
                                e.Remove<ContentControl>();
                            });
        }
    }
}
