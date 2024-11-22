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
        /// <summary>
        /// Represents a button click event in the ECS system.
        /// Contains the original Avalonia event data.
        /// </summary>
        /// <param name="sender">The object that triggered the event</param>
        /// <param name="args">The event arguments from Avalonia</param>
        public struct Click(object sender, RoutedEventArgs args);
        public struct Activated(object sender, EventArgs args);
        public struct AttachedToLogicalTree(object sender, LogicalTreeAttachmentEventArgs args);
        public struct AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs args);
        public struct Closed(object sender, EventArgs args);
        public struct Closing(object sender, EventArgs args);
        public struct DataContextChanged(object sender, EventArgs e);
        public struct Deactivated(object sender, EventArgs args);
        public struct DetachedFromLogicalTree(object sender, LogicalTreeAttachmentEventArgs args);
        public struct DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs args);
        public struct DoubleTapped(object sender, TappedEventArgs args);
        public struct EffectiveViewportChanged(object sender, EffectiveViewportChangedEventArgs args);
        public struct GotFocus(object sender, GotFocusEventArgs args);
        public struct Initialized(object sender, EventArgs args);
        public struct KeyDown(object sender, KeyEventArgs args);
        public struct KeyUp(object sender, KeyEventArgs args);
        public struct LayoutUpdated(object sender, EventArgs args);
        public struct LostFocus(object sender, RoutedEventArgs args);
        public struct Opened(object sender, EventArgs args);
        public struct PointerCaptureLost(object sender, PointerCaptureLostEventArgs args);
        public struct PointerEnter(object sender, PointerEventArgs args);
        public struct PointerLeave(object sender, PointerEventArgs args);
        public struct PointerMoved(object sender, PointerEventArgs args);
        public struct PointerPressed(object sender, PointerPressedEventArgs args);
        public struct PointerReleased(object sender, PointerReleasedEventArgs args);
        public struct PointerWheelChanged(object sender, PointerWheelEventArgs args);
        public struct PositionChanged(object sender, PixelPointEventArgs args);
        public struct PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs args);
        public struct ResourcesChanged(object sender, ResourcesChangedEventArgs args);
        public struct Tapped(object sender, TappedEventArgs args);
        public struct TemplateApplied(object sender, TemplateAppliedEventArgs args);
        public struct TextInput(object sender, TextInputEventArgs args);
        public struct TextInputMethodClientRequested(object sender, TextInputMethodClientRequestedEventArgs args);
        public void InitModule(World world)
        {
            // Register module with world. The module entity will be created with the
            // same hierarchy as the .NET namespaces (e.g. Avalonia.Flecs.Core.ECS.Module)
            world.Module<Module>();

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
                        e.Emit(new Closed(sender, args));
                    };

                    window.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Emit(new DataContextChanged(sender, args));
                    };

                    window.Deactivated += (object sender, EventArgs args) =>
                    {
                        e.Emit(new Deactivated(sender, args));
                    };

                    window.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromLogicalTree(sender, args));
                    };

                    window.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromVisualTree(sender, args));
                    };

                    window.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new DoubleTapped(sender, args));
                    };

                    window.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Emit(new EffectiveViewportChanged(sender, args));
                    };

                    window.PositionChanged += (object sender, PixelPointEventArgs args) =>
                    {
                        e.Emit(new PositionChanged(sender, args));
                    };

                    window.GotFocus += (object sender, GotFocusEventArgs args) =>
                                {
                                    e.Emit(new GotFocus(sender, args));
                                };

                    window.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Emit(new Initialized(sender, args));
                    };

                    window.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyDown(sender, args));
                    };

                    window.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyUp(sender, args));
                    };

                    window.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Emit(new LayoutUpdated(sender, args));
                    };

                    window.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Emit(new LostFocus(sender, args));
                    };

                    window.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Emit(new PointerCaptureLost(sender, args));
                    };

                    window.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerEnter(sender, args));
                    };

                    window.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerLeave(sender, args));
                    };

                    window.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerMoved(sender, args));
                    };

                    window.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Emit(new PointerPressed(sender, args));
                    };

                    window.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Emit(new PointerReleased(sender, args));
                    };

                    window.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Emit(new PointerWheelChanged(sender, args));
                    };

                    window.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Emit(new PropertyChanged(sender, args));
                    };

                    window.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Emit(new ResourcesChanged(sender, args));
                    };

                    window.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new Tapped(sender, args));
                    };

                    window.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Emit(new TextInput(sender, args));
                    };

                    window.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Emit(new TextInputMethodClientRequested(sender, args));
                    };

                    window.TemplateApplied += (object sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Emit(new TemplateApplied(sender, args));
                    };

                    window.Opened += (object sender, EventArgs args) =>
                    {
                        e.Emit(new Opened(sender, args));
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

                                // Adding event handlers
                                // https://reference.avaloniaui.net/api/Avalonia.Controls/TextBlock/#Events

                                textBlock.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Emit(new AttachedToLogicalTree(sender, args));
                                };

                                textBlock.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Emit(new AttachedToVisualTree(sender, args));
                                };

                                textBlock.DataContextChanged += (object sender, EventArgs args) =>
                                {
                                    e.Emit(new DataContextChanged(sender, args));
                                };

                                textBlock.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Emit(new DetachedFromLogicalTree(sender, args));
                                };

                                textBlock.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Emit(new DetachedFromVisualTree(sender, args));
                                };

                                textBlock.DoubleTapped += (object sender, TappedEventArgs args) =>
                                {
                                    e.Emit(new DoubleTapped(sender, args));
                                };

                                textBlock.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                                {
                                    e.Emit(new EffectiveViewportChanged(sender, args));
                                };

                                textBlock.GotFocus += (object sender, GotFocusEventArgs args) =>
                                {
                                    e.Emit(new GotFocus(sender, args));
                                };

                                textBlock.Initialized += (object sender, EventArgs args) =>
                                {
                                    e.Emit(new Initialized(sender, args));
                                };

                                textBlock.KeyDown += (object sender, KeyEventArgs args) =>
                                {
                                    e.Emit(new KeyDown(sender, args));
                                };

                                textBlock.KeyUp += (object sender, KeyEventArgs args) =>
                                {
                                    e.Emit(new KeyUp(sender, args));
                                };

                                textBlock.LayoutUpdated += (object sender, EventArgs args) =>
                                {
                                    e.Emit(new LayoutUpdated(sender, args));
                                };

                                textBlock.LostFocus += (object sender, RoutedEventArgs args) =>
                                {
                                    e.Emit(new LostFocus(sender, args));
                                };

                                textBlock.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                                {
                                    e.Emit(new PointerCaptureLost(sender, args));
                                };

                                textBlock.PointerEntered += (object sender, PointerEventArgs args) =>
                                {
                                    e.Emit(new PointerEnter(sender, args));
                                };

                                textBlock.PointerExited += (object sender, PointerEventArgs args) =>
                                {
                                    e.Emit(new PointerLeave(sender, args));
                                };

                                textBlock.PointerMoved += (object sender, PointerEventArgs args) =>
                                {
                                    e.Emit(new PointerMoved(sender, args));
                                };

                                textBlock.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                                {
                                    e.Emit(new PointerPressed(sender, args));
                                };

                                textBlock.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                                {
                                    e.Emit(new PointerReleased(sender, args));
                                };

                                textBlock.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                                {
                                    e.Emit(new PointerWheelChanged(sender, args));
                                };

                                textBlock.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                                {
                                    e.Emit(new PropertyChanged(sender, args));
                                };

                                textBlock.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                                {
                                    e.Emit(new ResourcesChanged(sender, args));
                                };

                                textBlock.Tapped += (object sender, TappedEventArgs args) =>
                                {
                                    e.Emit(new Tapped(sender, args));
                                };

                                textBlock.TextInput += (object sender, TextInputEventArgs args) =>
                                {
                                    e.Emit(new TextInput(sender, args));
                                };

                                textBlock.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                                {
                                    e.Emit(new TextInputMethodClientRequested(sender, args));
                                };
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

                    menu.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Emit(new AttachedToLogicalTree(sender, args));
                                };

                    menu.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new AttachedToVisualTree(sender, args));
                    };

                    menu.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Emit(new DataContextChanged(sender, args));
                    };

                    menu.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromLogicalTree(sender, args));
                    };

                    menu.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromVisualTree(sender, args));
                    };

                    menu.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new DoubleTapped(sender, args));
                    };

                    menu.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Emit(new EffectiveViewportChanged(sender, args));
                    };

                    menu.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Emit(new GotFocus(sender, args));
                    };

                    menu.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Emit(new Initialized(sender, args));
                    };

                    menu.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyDown(sender, args));
                    };

                    menu.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyUp(sender, args));
                    };

                    menu.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Emit(new LayoutUpdated(sender, args));
                    };

                    menu.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Emit(new LostFocus(sender, args));
                    };

                    menu.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Emit(new PointerCaptureLost(sender, args));
                    };

                    menu.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerEnter(sender, args));
                    };

                    menu.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerLeave(sender, args));
                    };

                    menu.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerMoved(sender, args));
                    };

                    menu.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Emit(new PointerPressed(sender, args));
                    };

                    menu.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Emit(new PointerReleased(sender, args));
                    };

                    menu.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Emit(new PointerWheelChanged(sender, args));
                    };

                    menu.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Emit(new PropertyChanged(sender, args));
                    };

                    menu.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Emit(new ResourcesChanged(sender, args));
                    };

                    menu.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new Tapped(sender, args));
                    };

                    menu.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Emit(new TextInput(sender, args));
                    };

                    menu.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Emit(new TextInputMethodClientRequested(sender, args));
                    };


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

                    menuItem.Click += (object sender, RoutedEventArgs args) =>
                                        {
                                            e.Emit(new Click(sender, args));
                                        };

                    menuItem.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new AttachedToLogicalTree(sender, args));
                    };

                    menuItem.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new AttachedToVisualTree(sender, args));
                    };

                    menuItem.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Emit(new DataContextChanged(sender, args));
                    };

                    menuItem.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromLogicalTree(sender, args));
                    };

                    menuItem.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromVisualTree(sender, args));
                    };

                    menuItem.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new DoubleTapped(sender, args));
                    };

                    menuItem.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Emit(new EffectiveViewportChanged(sender, args));
                    };

                    menuItem.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Emit(new GotFocus(sender, args));
                    };

                    menuItem.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Emit(new Initialized(sender, args));
                    };

                    menuItem.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyDown(sender, args));
                    };

                    menuItem.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyUp(sender, args));
                    };

                    menuItem.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Emit(new LayoutUpdated(sender, args));
                    };

                    menuItem.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Emit(new LostFocus(sender, args));
                    };

                    menuItem.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Emit(new PointerCaptureLost(sender, args));
                    };

                    menuItem.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerEnter(sender, args));
                    };

                    menuItem.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerLeave(sender, args));
                    };

                    menuItem.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerMoved(sender, args));
                    };

                    menuItem.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Emit(new PointerPressed(sender, args));
                    };

                    menuItem.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Emit(new PointerReleased(sender, args));
                    };

                    menuItem.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Emit(new PointerWheelChanged(sender, args));
                    };

                    menuItem.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Emit(new PropertyChanged(sender, args));
                    };

                    menuItem.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Emit(new ResourcesChanged(sender, args));
                    };

                    menuItem.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new Tapped(sender, args));
                    };

                    menuItem.TemplateApplied += (object sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Emit(new TemplateApplied(sender, args));
                    };

                    menuItem.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Emit(new TextInput(sender, args));
                    };

                    menuItem.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Emit(new TextInputMethodClientRequested(sender, args));
                    };

                });

            world.Component<Button>("Button")
                .OnSet((Entity e, ref Button button) =>
                {
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
                    // Adding event handlers
                    // https://reference.avaloniaui.net/api/Avalonia.Controls/Button/#Events
                    button.Click += (object sender, RoutedEventArgs args) =>
                    {
                        e.Emit(new Click(sender, args));
                    };

                    button.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new AttachedToLogicalTree(sender, args));
                    };

                    button.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new AttachedToVisualTree(sender, args));
                    };

                    button.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Emit(new DataContextChanged(sender, args));
                    };

                    button.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromLogicalTree(sender, args));
                    };

                    button.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Emit(new DetachedFromVisualTree(sender, args));
                    };

                    button.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new DoubleTapped(sender, args));
                    };

                    button.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Emit(new EffectiveViewportChanged(sender, args));
                    };

                    button.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Emit(new GotFocus(sender, args));
                    };

                    button.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Emit(new Initialized(sender, args));
                    };

                    button.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyDown(sender, args));
                    };

                    button.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Emit(new KeyUp(sender, args));
                    };

                    button.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Emit(new LayoutUpdated(sender, args));
                    };

                    button.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Emit(new LostFocus(sender, args));
                    };

                    button.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Emit(new PointerCaptureLost(sender, args));
                    };

                    button.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerEnter(sender, args));
                    };

                    button.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerLeave(sender, args));
                    };

                    button.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Emit(new PointerMoved(sender, args));
                    };

                    button.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Emit(new PointerPressed(sender, args));
                    };

                    button.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Emit(new PointerReleased(sender, args));
                    };

                    button.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Emit(new PointerWheelChanged(sender, args));
                    };

                    button.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Emit(new PropertyChanged(sender, args));
                    };

                    button.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Emit(new ResourcesChanged(sender, args));
                    };

                    button.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Emit(new Tapped(sender, args));
                    };

                    button.TemplateApplied += (object sender, TemplateAppliedEventArgs args) =>
                    {
                        e.Emit(new TemplateApplied(sender, args));
                    };

                    button.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Emit(new TextInput(sender, args));
                    };

                    button.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Emit(new TextInputMethodClientRequested(sender, args));
                    };
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
    }
}