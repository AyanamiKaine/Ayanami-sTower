using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSScrollViewer : IFlecsModule
    {
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
