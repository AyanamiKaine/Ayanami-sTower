using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSControl>();
            world.Component<Control>("Control")
                .OnSet((Entity e, ref Control control) =>
                {

                    control.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToLogicalTree(sender, args));
                        e.Emit<AttachedToLogicalTree>();
                    };

                    control.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    control.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    control.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    control.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    control.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    control.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    control.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    control.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Set(new Initialized(sender, args));
                        e.Emit<Initialized>();
                    };

                    control.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    control.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    control.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    control.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    control.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    control.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    control.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    control.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    control.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    control.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    control.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    control.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    control.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Set(new ResourcesChanged(sender, args));
                        e.Emit<ResourcesChanged>();
                    };

                    control.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };


                    control.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    control.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Set(new TextInputMethodClientRequested(sender, args));
                        e.Emit<TextInputMethodClientRequested>();
                    };



                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        if (parent.Get<Panel>().Children.Contains(control))
                        {
                            return;
                        }
                        
                        parent.Get<Panel>().Children.Add(control);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = control;
                    }

                }).OnRemove((Entity e, ref Control control) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(control);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });
        }
    }
}
