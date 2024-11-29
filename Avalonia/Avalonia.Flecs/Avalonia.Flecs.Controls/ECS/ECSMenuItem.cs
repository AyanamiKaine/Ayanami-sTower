using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Input.TextInput;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSMenuItem : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSMenuItem>();
            world.Component<MenuItem>("MenuItem")
                            .OnSet((Entity e, ref MenuItem menuItem) =>
                            {

                                e.Set<HeaderedSelectingItemsControl>(menuItem);

                                menuItem.Click += (object? sender, RoutedEventArgs args) =>
                                                                {
                                                                    e.Set(new Click(sender, args));
                                                                    e.Emit<Click>();
                                                                };

                                menuItem.AttachedToLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToLogicalTree(sender, args));
                                    e.Emit<AttachedToLogicalTree>();
                                };

                                menuItem.AttachedToVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToVisualTree(sender, args));
                                    e.Emit<AttachedToVisualTree>();
                                };

                                menuItem.DataContextChanged += (object? sender, EventArgs args) =>
                                {
                                    e.Set(new DataContextChanged(sender, args));
                                    e.Emit<DataContextChanged>();
                                };

                                menuItem.DetachedFromLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromLogicalTree(sender, args));
                                    e.Emit<DetachedFromLogicalTree>();
                                };

                                menuItem.DetachedFromVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromVisualTree(sender, args));
                                    e.Emit<DetachedFromVisualTree>();
                                };

                                menuItem.DoubleTapped += (object? sender, TappedEventArgs args) =>
                                {
                                    e.Set(new DoubleTapped(sender, args));
                                    e.Emit<DoubleTapped>();
                                };

                                menuItem.EffectiveViewportChanged += (object? sender, EffectiveViewportChangedEventArgs args) =>
                                {
                                    e.Set(new EffectiveViewportChanged(sender, args));
                                    e.Emit<EffectiveViewportChanged>();
                                };

                                menuItem.GotFocus += (object? sender, GotFocusEventArgs args) =>
                                {
                                    e.Set(new GotFocus(sender, args));
                                    e.Emit<GotFocus>();
                                };

                                menuItem.Initialized += (object? sender, EventArgs args) =>
                                {
                                    e.Set(new Initialized(sender, args));
                                    e.Emit<Initialized>();
                                };

                                menuItem.KeyDown += (object? sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyDown(sender, args));
                                    e.Emit<KeyDown>();
                                };

                                menuItem.KeyUp += (object? sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyUp(sender, args));
                                    e.Emit<KeyUp>();
                                };

                                menuItem.LayoutUpdated += (object? sender, EventArgs args) =>
                                {
                                    e.Set(new LayoutUpdated(sender, args));
                                    e.Emit<LayoutUpdated>();
                                };

                                menuItem.LostFocus += (object? sender, RoutedEventArgs args) =>
                                {
                                    e.Set(new LostFocus(sender, args));
                                    e.Emit<LostFocus>();
                                };

                                menuItem.PointerCaptureLost += (object? sender, PointerCaptureLostEventArgs args) =>
                                {
                                    e.Set(new PointerCaptureLost(sender, args));
                                    e.Emit<PointerCaptureLost>();
                                };

                                menuItem.PointerEntered += (object? sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerEnter(sender, args));
                                    e.Emit<PointerEnter>();
                                };

                                menuItem.PointerExited += (object? sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerLeave(sender, args));
                                    e.Emit<PointerLeave>();
                                };

                                menuItem.PointerMoved += (object? sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerMoved(sender, args));
                                    e.Emit<PointerMoved>();
                                };

                                menuItem.PointerPressed += (object? sender, PointerPressedEventArgs args) =>
                                {
                                    e.Set(new PointerPressed(sender, args));
                                    e.Emit<PointerPressed>();
                                };

                                menuItem.PointerReleased += (object? sender, PointerReleasedEventArgs args) =>
                                {
                                    e.Set(new PointerReleased(sender, args));
                                    e.Emit<PointerReleased>();
                                };

                                menuItem.PointerWheelChanged += (object? sender, PointerWheelEventArgs args) =>
                                {
                                    e.Set(new PointerWheelChanged(sender, args));
                                    e.Emit<PointerWheelChanged>();
                                };

                                menuItem.PropertyChanged += (object? sender, AvaloniaPropertyChangedEventArgs args) =>
                                {
                                    e.Set(new PropertyChanged(sender, args));
                                    e.Emit<PropertyChanged>();
                                };

                                menuItem.ResourcesChanged += (object? sender, ResourcesChangedEventArgs args) =>
                                {
                                    e.Set(new ResourcesChanged(sender, args));
                                    e.Emit<ResourcesChanged>();
                                };

                                menuItem.Tapped += (object? sender, TappedEventArgs args) =>
                                {
                                    e.Set(new Tapped(sender, args));
                                    e.Emit<Tapped>();
                                };

                                menuItem.TemplateApplied += (object? sender, TemplateAppliedEventArgs args) =>
                                {
                                    e.Set(new TemplateApplied(sender, args));
                                    e.Emit<TemplateApplied>();
                                };

                                menuItem.TextInput += (object? sender, TextInputEventArgs args) =>
                                {
                                    e.Set(new TextInput(sender, args));
                                    e.Emit<TextInput>();
                                };

                                menuItem.TextInputMethodClientRequested += (object? sender, TextInputMethodClientRequestedEventArgs args) =>
                                {
                                    e.Set(new TextInputMethodClientRequested(sender, args));
                                    e.Emit<TextInputMethodClientRequested>();
                                };

                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<Menu>())
                                {
                                    parent.Get<Menu>().Items.Add(menuItem);
                                }
                                else if (parent.Has<MenuItem>())
                                {
                                    parent.Get<MenuItem>().Items.Add(menuItem);
                                }
                                else if (parent.Has<MenuFlyout>())
                                {
                                    parent.Get<MenuFlyout>().Items.Add(menuItem);
                                }

                            }).OnRemove((Entity e, ref MenuItem menuItem) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<Menu>())
                                {
                                    parent.Get<Menu>().Items.Remove(menuItem);
                                }
                                else if (parent.Has<MenuItem>())
                                {
                                    parent.Get<MenuItem>().Items.Remove(menuItem);
                                }
                            });

        }
    }
}
