using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSTextBox : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSTextBox>();
            world.Component<TextBox>("TextBox")
                            .OnSet((Entity e, ref TextBox textBox) =>
                            {
                                e.Set<TemplatedControl>(textBox);

                                // Adding event handlers
                                // https://reference.avaloniaui.net/api/Avalonia.Controls.TextBox/#Events

                                textBox.AttachedToLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToLogicalTree(sender, args));
                                    e.Emit<AttachedToLogicalTree>();
                                };

                                textBox.AttachedToVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new AttachedToVisualTree(sender, args));
                                    e.Emit<AttachedToVisualTree>();
                                };

                                textBox.DataContextChanged += (object? sender, EventArgs args) =>
                                {
                                    e.Set(new DataContextChanged(sender, args));
                                    e.Emit<DataContextChanged>();
                                };

                                textBox.DetachedFromLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromLogicalTree(sender, args));
                                    e.Emit<DetachedFromLogicalTree>();
                                };

                                textBox.DetachedFromVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                                {
                                    e.Set(new DetachedFromVisualTree(sender, args));
                                    e.Emit<DetachedFromVisualTree>();
                                };

                                textBox.DoubleTapped += (object? sender, TappedEventArgs args) =>
                                {
                                    e.Set(new DoubleTapped(sender, args));
                                    e.Emit<DoubleTapped>();
                                };

                                textBox.EffectiveViewportChanged += (object? sender, EffectiveViewportChangedEventArgs args) =>
                                {
                                    e.Set(new EffectiveViewportChanged(sender, args));
                                    e.Emit<EffectiveViewportChanged>();
                                };

                                textBox.GotFocus += (object? sender, GotFocusEventArgs args) =>
                                {
                                    e.Set(new GotFocus(sender, args));
                                    e.Emit<GotFocus>();
                                };

                                textBox.KeyDown += (object? sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyDown(sender, args));
                                    e.Emit<KeyDown>();
                                };

                                textBox.KeyUp += (object? sender, KeyEventArgs args) =>
                                {
                                    e.Set(new KeyUp(sender, args));
                                    e.Emit<KeyUp>();
                                };

                                textBox.LayoutUpdated += (object? sender, EventArgs args) =>
                                {
                                    e.Set(new LayoutUpdated(sender, args));
                                    e.Emit<LayoutUpdated>();
                                };

                                textBox.LostFocus += (object? sender, RoutedEventArgs args) =>
                                {
                                    e.Set(new LostFocus(sender, args));
                                    e.Emit<LostFocus>();
                                };

                                textBox.PointerCaptureLost += (object? sender, PointerCaptureLostEventArgs args) =>
                                {
                                    e.Set(new PointerCaptureLost(sender, args));
                                    e.Emit<PointerCaptureLost>();
                                };

                                textBox.PointerEntered += (object? sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerEnter(sender, args));
                                    e.Emit<PointerEnter>();
                                };

                                textBox.PointerExited += (object? sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerLeave(sender, args));
                                    e.Emit<PointerLeave>();
                                };

                                textBox.PointerMoved += (object? sender, PointerEventArgs args) =>
                                {
                                    e.Set(new PointerMoved(sender, args));
                                    e.Emit<PointerMoved>();
                                };

                                textBox.PointerPressed += (object? sender, PointerPressedEventArgs args) =>
                                {
                                    e.Set(new PointerPressed(sender, args));
                                    e.Emit<PointerPressed>();
                                };

                                textBox.PointerReleased += (object? sender, PointerReleasedEventArgs args) =>
                                {
                                    e.Set(new PointerReleased(sender, args));
                                    e.Emit<PointerReleased>();
                                };

                                textBox.PointerWheelChanged += (object? sender, PointerWheelEventArgs args) =>
                                {
                                    e.Set(new PointerWheelChanged(sender, args));
                                    e.Emit<PointerWheelChanged>();
                                };

                                textBox.PropertyChanged += (object? sender, AvaloniaPropertyChangedEventArgs args) =>
                                {
                                    e.Set(new PropertyChanged(sender, args));
                                    e.Emit<PropertyChanged>();
                                };

                                textBox.ResourcesChanged += (object? sender, ResourcesChangedEventArgs args) =>
                                {
                                    e.Set(new ResourcesChanged(sender, args));
                                    e.Emit<ResourcesChanged>();
                                };

                                textBox.Tapped += (object? sender, TappedEventArgs args) =>
                                {
                                    e.Set(new Tapped(sender, args));
                                    e.Emit<Tapped>();
                                };

                                textBox.TextInput += (object? sender, TextInputEventArgs args) =>
                                {
                                    e.Set(new TextInput(sender, args));
                                    e.Emit<TextInput>();
                                };

                                textBox.TextChanging += (object? sender, TextChangingEventArgs args) =>
                                {
                                    e.Set(new TextChanging(sender, args));
                                    e.Emit<TextChanging>();
                                };

                                textBox.TextChanged += (object? sender, TextChangedEventArgs args) =>
                                {
                                    e.Set(new TextChanged(sender, args));
                                    e.Emit<TextChanged>();
                                };

                                textBox.TextInputMethodClientRequested += (object? sender, TextInputMethodClientRequestedEventArgs args) =>
                                {
                                    e.Set(new TextInputMethodClientRequested(sender, args));
                                    e.Emit<TextInputMethodClientRequested>();
                                };
                            }).OnRemove((Entity e, ref TextBox textBox) =>
                            {
                                e.Remove<TemplatedControl>();
                            });
        }
    }
}
