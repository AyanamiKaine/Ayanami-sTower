using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSMenu : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSMenu>();
            world.Component<Menu>("Menu")
                            .OnSet((Entity e, ref Menu menu) =>
                            {
                                menu.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToLogicalTree(sender, args));
                                    e.Emit<AttachedToLogicalTree>();
                                };

                                menu.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToVisualTree(sender, args));
                                    e.Emit<AttachedToVisualTree>();
                                };

                                menu.DataContextChanged += (object sender, EventArgs args) =>
                                {
                                    e.Set(new DataContextChanged(sender, args));
                                    e.Emit<DataContextChanged>();
                                };

                                menu.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromLogicalTree(sender, args));
                                    e.Emit<DetachedFromLogicalTree>();
                                };

                                menu.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromVisualTree(sender, args));
                                    e.Emit<DetachedFromVisualTree>();
                                };

                                menu.DoubleTapped += (object sender, TappedEventArgs args) =>
                                {
                                    e.Set(new DoubleTapped(sender, args));
                                    e.Emit<DoubleTapped>();
                                };

                                menu.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                                {
                                    e.Set(new EffectiveViewportChanged(sender, args));
                                    e.Emit<EffectiveViewportChanged>();
                                };

                                menu.GotFocus += (object sender, GotFocusEventArgs args) =>
                                {
                                    e.Set(new GotFocus(sender, args));
                                    e.Emit<GotFocus>();
                                };

                                menu.Initialized += (object sender, EventArgs args) =>
                                {
                                    e.Set(new Initialized(sender, args));
                                    e.Emit<Initialized>();
                                };

                                menu.KeyDown += (object sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyDown(sender, args));
                                    e.Emit<KeyDown>();
                                };

                                menu.KeyUp += (object sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyUp(sender, args));
                                    e.Emit<KeyUp>();
                                };

                                menu.LayoutUpdated += (object sender, EventArgs args) =>
                                {
                                    e.Set(new LayoutUpdated(sender, args));
                                    e.Emit<LayoutUpdated>();
                                };

                                menu.LostFocus += (object sender, RoutedEventArgs args) =>
                                {
                                    e.Set(new LostFocus(sender, args));
                                    e.Emit<LostFocus>();
                                };

                                menu.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                                {
                                    e.Set(new PointerCaptureLost(sender, args));
                                    e.Emit<PointerCaptureLost>();
                                };

                                menu.PointerEntered += (object sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerEnter(sender, args));
                                    e.Emit<PointerEnter>();
                                };

                                menu.PointerExited += (object sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerLeave(sender, args));
                                    e.Emit<PointerLeave>();
                                };

                                menu.PointerMoved += (object sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerMoved(sender, args));
                                    e.Emit<PointerMoved>();
                                };

                                menu.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                                {
                                    e.Set(new PointerPressed(sender, args));
                                    e.Emit<PointerPressed>();
                                };

                                menu.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                                {
                                    e.Set(new PointerReleased(sender, args));
                                    e.Emit<PointerReleased>();
                                };

                                menu.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                                {
                                    e.Set(new PointerWheelChanged(sender, args));
                                    e.Emit<PointerWheelChanged>();
                                };

                                menu.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                                {
                                    e.Set(new PropertyChanged(sender, args));
                                    e.Emit<PropertyChanged>();
                                };

                                menu.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                                {
                                    e.Set(new ResourcesChanged(sender, args));
                                    e.Emit<ResourcesChanged>();
                                };

                                menu.Tapped += (object sender, TappedEventArgs args) =>
                                {
                                    e.Set(new Tapped(sender, args));
                                    e.Emit<Tapped>();
                                };

                                menu.TextInput += (object sender, TextInputEventArgs args) =>
                                {
                                    e.Set(new TextInput(sender, args));
                                    e.Emit<TextInput>();
                                };

                                menu.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                                {
                                    e.Set(new TextInputMethodClientRequested(sender, args));
                                    e.Emit<TextInputMethodClientRequested>();
                                };
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<ContentControl>())
                                {
                                    parent.Get<ContentControl>().Content = menu;
                                }

                                if (parent.Has<Panel>())
                                {
                                    parent.Get<Panel>().Children.Add(menu);
                                }

                                DockPanel.SetDock(menu, Dock.Top);

                            }).OnRemove((Entity e, ref Menu menu) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<ContentControl>())
                                {
                                    parent.Get<ContentControl>().Content = null;
                                }

                                if (parent.Has<Panel>())
                                {
                                    parent.Get<Panel>().Children.Remove(menu);
                                }
                            });
        }
    }
}
