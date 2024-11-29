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

                                e.Set<ContentControl>(scrollViewer);
                                // Adding event handlers
                                // https://reference.avaloniaui.net/api/Avalonia.Controls.ScrollViewer/#Events

                                scrollViewer.AttachedToLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToLogicalTree(sender, args));
                                    e.Emit<AttachedToLogicalTree>();
                                };

                                scrollViewer.AttachedToVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToVisualTree(sender, args));
                                    e.Emit<AttachedToVisualTree>();
                                };

                                scrollViewer.DataContextChanged += (object? sender, EventArgs args) =>
                                {
                                    e.Set(new DataContextChanged(sender, args));
                                    e.Emit<DataContextChanged>();
                                };

                                scrollViewer.DetachedFromLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromLogicalTree(sender, args));
                                    e.Emit<DetachedFromLogicalTree>();
                                };

                                scrollViewer.DetachedFromVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromVisualTree(sender, args));
                                    e.Emit<DetachedFromVisualTree>();
                                };

                                scrollViewer.DoubleTapped += (object? sender, TappedEventArgs args) =>
                                {
                                    e.Set(new DoubleTapped(sender, args));
                                    e.Emit<DoubleTapped>();
                                };

                                scrollViewer.EffectiveViewportChanged += (object? sender, EffectiveViewportChangedEventArgs args) =>
                                {
                                    e.Set(new EffectiveViewportChanged(sender, args));
                                    e.Emit<EffectiveViewportChanged>();
                                };

                                scrollViewer.GotFocus += (object? sender, GotFocusEventArgs args) =>
                                {
                                    e.Set(new GotFocus(sender, args));
                                    e.Emit<GotFocus>();
                                };
                            }).OnRemove((Entity e, ref ScrollViewer scrollViewer) =>
                            {
                                e.Remove<ContentControl>();
                            });
        }
    }
}
