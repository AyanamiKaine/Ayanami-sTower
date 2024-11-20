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

        public struct AttachedToLogicalTree(object sender, LogicalTreeAttachmentEventArgs args);
        public struct AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs args);
        public struct DataContextChanged(object sender, EventArgs e);
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
        public struct PointerCaptureLost(object sender, PointerCaptureLostEventArgs args);
        public struct PointerEnter(object sender, PointerEventArgs args);
        public struct PointerLeave(object sender, PointerEventArgs args);
        public struct PointerMoved(object sender, PointerEventArgs args);
        public struct PointerPressed(object sender, PointerPressedEventArgs args);
        public struct PointerReleased(object sender, PointerReleasedEventArgs args);
        public struct PointerWheelChanged(object sender, PointerWheelEventArgs args);
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
            world.Component<Window>("Window");

            RegisterPanelComponents(world);

            world.Component<TextBlock>("TextBlock")
                            .OnSet((Entity e, ref TextBlock textBlock) =>
                            {
                                var parent = e.Parent();
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
                            });

            world.Component<Button>("Button")
                .OnSet((Entity e, ref Button button) =>
                {
                    var parent = e.Parent();
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
                });



        }

        private static void RegisterPanelComponents(World world)
        {
            // All layout components need to work with the root window

            world.Component<DockPanel>("DockPanel")
                .OnSet((Entity e, ref DockPanel dockPanel) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = dockPanel;
                    }
                });

            world.Component<Canvas>("Canvas")
                .OnSet((Entity e, ref Canvas canvas) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = canvas;
                    }
                });

            world.Component<StackPanel>("StackPanel")
                .OnSet((Entity e, ref StackPanel stackPanel) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = stackPanel;
                    }
                });

            world.Component<Grid>("Grid")
                .OnSet((Entity e, ref Grid grid) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = grid;
                    }
                });

            world.Component<WrapPanel>("WrapPanel")
                .OnSet((Entity e, ref WrapPanel wrapPanel) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = wrapPanel;
                    }
                });

            world.Component<RelativePanel>("RelativePanel")
                .OnSet((Entity e, ref RelativePanel relativePanel) =>
                {
                    var parent = e.Parent();
                    if (parent.Has<Window>())
                    {
                        parent.Get<Window>().Content = relativePanel;
                    }
                });
        }
    }
}