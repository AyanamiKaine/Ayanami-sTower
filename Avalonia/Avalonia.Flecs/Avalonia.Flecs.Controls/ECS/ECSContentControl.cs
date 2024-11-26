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
    public class ECSContentControl : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSContentControl>();
            world.Component<ContentControl>("ContentControl")
                .OnSet((Entity e, ref ContentControl contentControl) =>
                {


                    contentControl.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                       {
                           e.Set(new AttachedToLogicalTree(sender, args));
                           e.Emit<AttachedToLogicalTree>();
                       };

                    contentControl.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    contentControl.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    contentControl.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    contentControl.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    contentControl.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    contentControl.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    contentControl.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    contentControl.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Set(new Initialized(sender, args));
                        e.Emit<Initialized>();
                    };

                    contentControl.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    contentControl.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    contentControl.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    contentControl.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    contentControl.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    contentControl.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    contentControl.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    contentControl.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    contentControl.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    contentControl.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    contentControl.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    contentControl.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    contentControl.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Set(new ResourcesChanged(sender, args));
                        e.Emit<ResourcesChanged>();
                    };

                    contentControl.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };


                    contentControl.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    contentControl.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Set(new TextInputMethodClientRequested(sender, args));
                        e.Emit<TextInputMethodClientRequested>();
                    };

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    else if (parent.Has<Panel>())
                    {
                        if (parent.Get<Panel>().Children.Contains(contentControl))
                        {
                            return;
                        }
                        parent.Get<Panel>().Children.Add(contentControl);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = contentControl;
                    }
                }).OnRemove((Entity e, ref ContentControl contentControl) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(contentControl);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });
        }
    }
}
