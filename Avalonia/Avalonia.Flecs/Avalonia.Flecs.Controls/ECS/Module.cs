using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input.TextInput;
namespace Avalonia.Flecs.Controls.ECS
{
    // Modules need to implement the IFlecsModule interface
    public struct Module : IFlecsModule
    {

        ////RELATIONSHIPS
        public struct InnerRightContent { };
        public struct InnerLeftContent { };
        public struct KeyBindings { };

        /////EVENTS

        /// <summary>
        /// Represents a button click event in the ECS system.
        /// Contains the original Avalonia event data.
        /// </summary>
        /// <param name="Sender">The object that triggered the event</param>
        /// <param name="args">The event arguments from Avalonia</param>
        public record struct Click(object Sender, RoutedEventArgs Args);
        public record struct Activated(object Sender, EventArgs Args);
        public record struct AttachedToLogicalTree(object Sender, LogicalTreeAttachmentEventArgs Args);
        public record struct AttachedToVisualTree(object Sender, VisualTreeAttachmentEventArgs Args);
        public record struct Closed(object Sender, EventArgs Args);
        public record struct Closing(object Sender, EventArgs Args);
        public record struct DataContextChanged(object Sender, EventArgs E);
        public record struct Deactivated(object Sender, EventArgs Args);
        public record struct DetachedFromLogicalTree(object Sender, LogicalTreeAttachmentEventArgs Args);
        public record struct DetachedFromVisualTree(object Sender, VisualTreeAttachmentEventArgs Args);
        public record struct DoubleTapped(object Sender, TappedEventArgs Args);
        public record struct EffectiveViewportChanged(object Sender, EffectiveViewportChangedEventArgs Args);
        public record struct GotFocus(object Sender, GotFocusEventArgs Args);
        public record struct Initialized(object Sender, EventArgs Args);
        public record struct KeyDown(object Sender, KeyEventArgs Args);
        public record struct KeyUp(object Sender, KeyEventArgs Args);
        public record struct LayoutUpdated(object Sender, EventArgs Args);
        public record struct LostFocus(object Sender, RoutedEventArgs Args);
        public record struct Opened(object Sender, EventArgs Args);
        public record struct PointerCaptureLost(object Sender, PointerCaptureLostEventArgs Args);
        public record struct PointerEnter(object Sender, PointerEventArgs Args);
        public record struct PointerLeave(object Sender, PointerEventArgs Args);
        public record struct PointerMoved(object Sender, PointerEventArgs Args);
        public record struct PointerPressed(object Sender, PointerPressedEventArgs Args);
        public record struct PointerReleased(object Sender, PointerReleasedEventArgs Args);
        public record struct PointerWheelChanged(object Sender, PointerWheelEventArgs Args);
        public record struct PositionChanged(object Sender, PixelPointEventArgs Args);
        public record struct PropertyChanged(object Sender, AvaloniaPropertyChangedEventArgs Args);
        public record struct ResourcesChanged(object Sender, ResourcesChangedEventArgs Args);
        public record struct Tapped(object Sender, TappedEventArgs Args);
        public record struct TemplateApplied(object Sender, TemplateAppliedEventArgs Args);
        public record struct TextInput(object Sender, TextInputEventArgs Args);
        public record struct TextInputMethodClientRequested(object Sender, TextInputMethodClientRequestedEventArgs Args);
        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();
            RegisterEventDataComponents(world);
            world.Component<ContentControl>("ContentControl")
                .OnSet((Entity e, ref ContentControl contentControl) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(contentControl);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(contentControl);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(contentControl);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(contentControl);
                    }
                    else if (parent.Has<Panel>())
                    {
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

            world.Component<AutoCompleteBox>("AutoCompleteBox")
                .OnSet((Entity e, ref AutoCompleteBox autoCompleteBox) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(autoCompleteBox);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(autoCompleteBox);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(autoCompleteBox);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(autoCompleteBox);
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(autoCompleteBox);
                    }
                    else if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = autoCompleteBox;
                    }
                })
                .OnRemove((Entity e, ref AutoCompleteBox autoCompleteBox) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(autoCompleteBox);
                    }
                });

            world.Component<Window>("Window")
                .OnSet((Entity e, ref Window window) =>
                {
                    e.Set<ContentControl>(window);

                    window.Activated += (object sender, EventArgs args) =>
                    {
                        e.Set(new Activated(sender, args));
                        e.Emit<Activated>();
                    };

                    window.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToLogicalTree(sender, args));
                        e.Emit<AttachedToLogicalTree>();
                    };

                    window.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    window.Closed += (object sender, EventArgs args) =>
                    {
                        e.Set(new Closed(sender, args));
                        e.Emit<Closed>();
                    };

                    window.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    window.Deactivated += (object sender, EventArgs args) =>
                    {
                        e.Set(new Deactivated(sender, args));
                        e.Emit<Deactivated>();
                    };

                    window.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    window.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    window.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    window.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    window.PositionChanged += (object sender, PixelPointEventArgs args) =>
                    {
                        e.Set(new PositionChanged(sender, args));
                        e.Emit<PositionChanged>();
                    };

                    window.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    window.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Set(new Initialized(sender, args));
                        e.Emit<Initialized>();
                    };

                    window.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    window.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    window.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    window.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    window.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    window.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    window.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    window.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    window.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    window.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    window.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    window.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    window.ResourcesChanged += (object? sender, ResourcesChangedEventArgs args) =>
                    {
                        // Can the sender even be null, is this possible?
                        if (sender != null)
                        {
                            e.Set(new ResourcesChanged(sender, args));
                            e.Emit<ResourcesChanged>();
                        }
                    };

                    window.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };

                    window.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    window.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Set(new TextInputMethodClientRequested(sender, args));
                        e.Emit<TextInputMethodClientRequested>();
                    };

                    window.TemplateApplied += (object sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Set(new TemplateApplied(sender, args));
                        e.Emit<TemplateApplied>();
                    };

                    window.Opened += (object sender, EventArgs args) =>
                    {
                        e.Set(new Opened(sender, args));
                        e.Emit<Opened>();
                    };
                })
                .OnRemove((Entity e, ref Window window) =>
                {
                    window.Close();
                });

            RegisterPanelComponents(world);


            world.Component<TextBlock>("TextBlock")
                            .OnSet((Entity e, ref TextBlock textBlock) =>
                            {
                                // Adding event handlers
                                // https://reference.avaloniaui.net/api/Avalonia.Controls/TextBlock/#Events

                                textBlock.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToLogicalTree(sender, args));
                                    e.Emit<AttachedToLogicalTree>();
                                };

                                textBlock.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToVisualTree(sender, args));
                                    e.Emit<AttachedToVisualTree>();
                                };

                                textBlock.DataContextChanged += (object sender, EventArgs args) =>
                                {
                                    e.Set(new DataContextChanged(sender, args));
                                    e.Emit<DataContextChanged>();
                                };

                                textBlock.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromLogicalTree(sender, args));
                                    e.Emit<DetachedFromLogicalTree>();
                                };

                                textBlock.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromVisualTree(sender, args));
                                    e.Emit<DetachedFromVisualTree>();
                                };

                                textBlock.DoubleTapped += (object sender, TappedEventArgs args) =>
                                {
                                    e.Set(new DoubleTapped(sender, args));
                                    e.Emit<DoubleTapped>();
                                };

                                textBlock.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                                {
                                    e.Set(new EffectiveViewportChanged(sender, args));
                                    e.Emit<EffectiveViewportChanged>();
                                };

                                textBlock.GotFocus += (object sender, GotFocusEventArgs args) =>
                                {
                                    e.Set(new GotFocus(sender, args));
                                    e.Emit<GotFocus>();
                                };

                                textBlock.Initialized += (object sender, EventArgs args) =>
                                {
                                    e.Set(new Initialized(sender, args));
                                    e.Emit<Initialized>();
                                };

                                textBlock.KeyDown += (object sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyDown(sender, args));
                                    e.Emit<KeyDown>();
                                };

                                textBlock.KeyUp += (object sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyUp(sender, args));
                                    e.Emit<KeyUp>();
                                };

                                textBlock.LayoutUpdated += (object sender, EventArgs args) =>
                                {
                                    e.Set(new LayoutUpdated(sender, args));
                                    e.Emit<LayoutUpdated>();
                                };

                                textBlock.LostFocus += (object sender, RoutedEventArgs args) =>
                                {
                                    e.Set(new LostFocus(sender, args));
                                    e.Emit<LostFocus>();
                                };

                                textBlock.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                                {
                                    e.Set(new PointerCaptureLost(sender, args));
                                    e.Emit<PointerCaptureLost>();
                                };

                                textBlock.PointerEntered += (object sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerEnter(sender, args));
                                    e.Emit<PointerEnter>();
                                };

                                textBlock.PointerExited += (object sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerLeave(sender, args));
                                    e.Emit<PointerLeave>();
                                };

                                textBlock.PointerMoved += (object sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerMoved(sender, args));
                                    e.Emit<PointerMoved>();
                                };

                                textBlock.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                                {
                                    e.Set(new PointerPressed(sender, args));
                                    e.Emit<PointerPressed>();
                                };

                                textBlock.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                                {
                                    e.Set(new PointerReleased(sender, args));
                                    e.Emit<PointerReleased>();
                                };

                                textBlock.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                                {
                                    e.Set(new PointerWheelChanged(sender, args));
                                    e.Emit<PointerWheelChanged>();
                                };

                                textBlock.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                                {
                                    e.Set(new PropertyChanged(sender, args));
                                    e.Emit<PropertyChanged>();
                                };

                                textBlock.ResourcesChanged += (object? sender, ResourcesChangedEventArgs args) =>
                                {
                                    e.Set(new ResourcesChanged(sender, args));
                                    e.Emit<ResourcesChanged>();
                                };

                                textBlock.Tapped += (object sender, TappedEventArgs args) =>
                                {
                                    e.Set(new Tapped(sender, args));
                                    e.Emit<Tapped>();
                                };

                                textBlock.TextInput += (object sender, TextInputEventArgs args) =>
                                {
                                    e.Set(new TextInput(sender, args));
                                    e.Emit<TextInput>();
                                };

                                textBlock.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                                {
                                    e.Set(new TextInputMethodClientRequested(sender, args));
                                    e.Emit<TextInputMethodClientRequested>();
                                };
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<StackPanel>())
                                {
                                    parent.Get<StackPanel>().Children.Add(textBlock);
                                }
                                else if (parent.Has<DockPanel>())
                                {
                                    parent.Get<DockPanel>().Children.Add(textBlock);
                                }
                                else if (parent.Has<Grid>())
                                {
                                    parent.Get<Grid>().Children.Add(textBlock);
                                }
                                else if (parent.Has<WrapPanel>())
                                {
                                    parent.Get<WrapPanel>().Children.Add(textBlock);
                                }
                                else if (parent.Has<ContentControl>())
                                {
                                    parent.Get<ContentControl>().Content = textBlock;
                                }


                            })
                .OnRemove((Entity e, ref TextBlock textBlock) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(textBlock);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                }); ;

            world.Component<Separator>("Separator")
                .OnSet((Entity e, ref Separator separator) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(separator);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(separator);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(separator);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(separator);
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(separator);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = separator;
                    }
                }).OnRemove((Entity e, ref Separator separator) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(separator);
                    }
                });

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

            world.Component<MenuItem>("MenuItem")
                .OnSet((Entity e, ref MenuItem menuItem) =>
                {
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



                }).OnRemove((Entity e, ref MenuItem menuItem) =>
                {

                    menuItem.Click += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new Click(sender, args));
                        e.Emit<Click>();
                    };

                    menuItem.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToLogicalTree(sender, args));
                        e.Emit<AttachedToLogicalTree>();
                    };

                    menuItem.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    menuItem.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    menuItem.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    menuItem.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    menuItem.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    menuItem.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    menuItem.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    menuItem.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Set(new Initialized(sender, args));
                        e.Emit<Initialized>();
                    };

                    menuItem.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    menuItem.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    menuItem.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    menuItem.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    menuItem.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    menuItem.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    menuItem.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    menuItem.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    menuItem.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    menuItem.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    menuItem.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    menuItem.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    menuItem.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Set(new ResourcesChanged(sender, args));
                        e.Emit<ResourcesChanged>();
                    };

                    menuItem.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };

                    menuItem.TemplateApplied += (object sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Set(new TemplateApplied(sender, args));
                        e.Emit<TemplateApplied>();
                    };

                    menuItem.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    menuItem.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
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
                        parent.Get<Menu>().Items.Remove(menuItem);
                    }
                    else if (parent.Has<MenuItem>())
                    {
                        parent.Get<MenuItem>().Items.Remove(menuItem);
                    }
                });

            world.Component<Button>("Button")
                .OnSet((Entity e, ref Button button) =>
                {
                    // Adding event handlers
                    // https://reference.avaloniaui.net/api/Avalonia.Controls/Button/#Events
                    button.Click += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new Click(sender, args));
                        e.Emit<Click>();
                    };

                    button.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToLogicalTree(sender, args));
                        e.Emit<AttachedToLogicalTree>();
                    };

                    button.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    button.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    button.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    button.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    button.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    button.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    button.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    button.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Set(new Initialized(sender, args));
                        e.Emit<Initialized>();
                    };

                    button.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    button.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    button.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    button.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    button.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    button.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    button.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    button.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    button.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    button.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    button.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    button.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    button.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Set(new ResourcesChanged(sender, args));
                        e.Emit<ResourcesChanged>();
                    };

                    button.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };

                    button.TemplateApplied += (object sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Set(new TemplateApplied(sender, args));
                        e.Emit<TemplateApplied>();
                    };

                    button.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    button.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Set(new TextInputMethodClientRequested(sender, args));
                        e.Emit<TextInputMethodClientRequested>();
                    };
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(button);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(button);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(button);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(button);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = button;
                    }
                }).OnRemove((Entity e, ref Button button) =>
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
                        parent.Get<Panel>().Children.Remove(button);
                    }
                });


            world.Component<TextBox>("TextBox")
                .OnSet((Entity e, ref TextBox textBox) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(textBox);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(textBox);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(textBox);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(textBox);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = textBox;
                    }

                    // Adding event handlers
                    // https://reference.avaloniaui.net/api/Avalonia.Controls.TextBox/#Events

                    textBox.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToLogicalTree(sender, args));
                        e.Emit<AttachedToLogicalTree>();
                    };

                    textBox.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    textBox.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    textBox.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    textBox.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    textBox.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    textBox.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    textBox.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    textBox.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    textBox.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    textBox.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    textBox.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    textBox.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    textBox.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    textBox.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    textBox.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    textBox.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    textBox.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    textBox.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    textBox.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    textBox.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Set(new ResourcesChanged(sender, args));
                        e.Emit<ResourcesChanged>();
                    };

                    textBox.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };

                    textBox.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    textBox.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Set(new TextInputMethodClientRequested(sender, args));
                        e.Emit<TextInputMethodClientRequested>();
                    };
                }).OnRemove((Entity e, ref TextBox textBox) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(textBox);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });

            world.Component<ScrollViewer>("ScrollViewer")
                .OnSet((Entity e, ref ScrollViewer scrollViewer) =>
                {
                    // Adding event handlers
                    // https://reference.avaloniaui.net/api/Avalonia.Controls.ScrollViewer/#Events

                    scrollViewer.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToLogicalTree(sender, args));
                        e.Emit<AttachedToLogicalTree>();
                    };

                    scrollViewer.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    scrollViewer.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    scrollViewer.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    scrollViewer.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    scrollViewer.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    scrollViewer.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    scrollViewer.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(scrollViewer);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(scrollViewer);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(scrollViewer);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(scrollViewer);
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = scrollViewer;
                    }

                }).OnRemove((Entity e, ref ScrollViewer scrollViewer) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Remove(scrollViewer);
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = null;
                    }
                });

            world.Component<ItemsControl>("ItemsControl")
                .OnSet((Entity e, ref ItemsControl itemsControl) =>
                {
                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<StackPanel>())
                    {
                        parent.Get<StackPanel>().Children.Add(itemsControl);
                    }
                    else if (parent.Has<DockPanel>())
                    {
                        parent.Get<DockPanel>().Children.Add(itemsControl);
                    }
                    else if (parent.Has<Grid>())
                    {
                        parent.Get<Grid>().Children.Add(itemsControl);
                    }
                    else if (parent.Has<WrapPanel>())
                    {
                        parent.Get<WrapPanel>().Children.Add(itemsControl);
                    }
                    else if (parent.Has<ScrollViewer>())
                    {
                        parent.Get<ScrollViewer>().Content = itemsControl;
                    }
                    else if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = itemsControl;
                    }
                });
        }

        private static void RegisterPanelComponents(World world)
        {
            // All layout components need to work with the root window

            world.Component<DockPanel>("DockPanel")
                .OnSet((Entity e, ref DockPanel dockPanel) =>
                {
                    e.Set<Panel>(dockPanel);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = dockPanel;
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(dockPanel);
                    }
                }).OnRemove((Entity e, ref DockPanel dockPanel) =>
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
                        parent.Get<Panel>().Children.Remove(dockPanel);
                    }
                });

            world.Component<Canvas>("Canvas")
                .OnSet((Entity e, ref Canvas canvas) =>
                {
                    e.Set<Panel>(canvas);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = canvas;
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(canvas);
                    }
                }).OnRemove((Entity e, ref Canvas canvas) =>
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
                        parent.Get<Panel>().Children.Remove(canvas);
                    }
                });

            world.Component<StackPanel>("StackPanel")
                .OnSet((Entity e, ref StackPanel stackPanel) =>
                {
                    e.Set<Panel>(stackPanel);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = stackPanel;
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(stackPanel);
                    }
                }).OnRemove((Entity e, ref StackPanel stackPanel) =>
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
                        parent.Get<Panel>().Children.Remove(stackPanel);
                    }
                });

            world.Component<Grid>("Grid")
                .OnSet((Entity e, ref Grid grid) =>
                {
                    e.Set<Panel>(grid);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = grid;
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(grid);
                    }
                }).OnRemove((Entity e, ref Grid grid) =>
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
                        parent.Get<Panel>().Children.Remove(grid);
                    }
                });

            world.Component<WrapPanel>("WrapPanel")
                .OnSet((Entity e, ref WrapPanel wrapPanel) =>
                {
                    e.Set<Panel>(wrapPanel);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = wrapPanel;
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(wrapPanel);
                    }
                }).OnRemove((Entity e, ref WrapPanel wrapPanel) =>
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
                        parent.Get<Panel>().Children.Remove(wrapPanel);
                    }
                });

            world.Component<RelativePanel>("RelativePanel")
                .OnSet((Entity e, ref RelativePanel relativePanel) =>
                {
                    e.Set<Panel>(relativePanel);

                    var parent = e.Parent();
                    if (parent == 0)
                    {
                        return;
                    }
                    if (parent.Has<ContentControl>())
                    {
                        parent.Get<ContentControl>().Content = relativePanel;
                    }
                    else if (parent.Has<Panel>())
                    {
                        parent.Get<Panel>().Children.Add(relativePanel);
                    }

                })
                .OnRemove((Entity e, ref RelativePanel relativePanel) =>
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
                        parent.Get<Panel>().Children.Remove(relativePanel);
                    }
                });
        }

        public static void RegisterRelationship(World world)
        {
            world.Component<InnerLeftContent>("InnerLeftContent")
                .OnAdd((Entity e, ref InnerLeftContent innerLeftContent) =>
                {
                    Console.WriteLine("InnerLeftContent added");
                });
            world.Component<InnerRightContent>("InnerRightContent");
            world.Component<KeyBindings>("InnerTopContent");
        }

        public static void RegisterEventDataComponents(World world)
        {
            world.Component<Click>("Click");
            world.Component<Activated>("Activated");
            world.Component<AttachedToLogicalTree>("AttachedToLogicalTree");
            world.Component<AttachedToVisualTree>("AttachedToVisualTree");
            world.Component<Closed>("Closed");
            world.Component<Closing>("Closing");
            world.Component<DataContextChanged>("DataContextChanged");
            world.Component<Deactivated>("Deactivated");
            world.Component<DetachedFromLogicalTree>("DetachedFromLogicalTree");
            world.Component<DetachedFromVisualTree>("DetachedFromVisualTree");
            world.Component<DoubleTapped>("DoubleTapped");
            world.Component<EffectiveViewportChanged>("EffectiveViewportChanged");
            world.Component<GotFocus>("GotFocus");
            world.Component<Initialized>("Initialized");
            world.Component<KeyDown>("KeyDown");
            world.Component<KeyUp>("KeyUp");
            world.Component<LayoutUpdated>("LayoutUpdated");
            world.Component<LostFocus>("LostFocus");
            world.Component<Opened>("Opened");
            world.Component<PointerCaptureLost>("PointerCaptureLost");
            world.Component<PointerEnter>("PointerEnter");
            world.Component<PointerLeave>("PointerLeave");
            world.Component<PointerMoved>("PointerMoved");
            world.Component<PointerPressed>("PointerPressed");
            world.Component<PointerReleased>("PointerReleased");
            world.Component<PointerWheelChanged>("PointerWheelChanged");
            world.Component<PositionChanged>("PositionChanged");
            world.Component<PropertyChanged>("PropertyChanged");
            world.Component<ResourcesChanged>("ResourcesChanged");
            world.Component<Tapped>("Tapped");
            world.Component<TemplateApplied>("TemplateApplied");
            world.Component<TextInput>("TextInput");
            world.Component<TextInputMethodClientRequested>("TextInputMethodClientRequested");
        }
    }
}