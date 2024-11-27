using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Input.TextInput;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSInputElement : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSInputElement>();
            world.Component<InputElement>("InputElement")
                .OnSet((Entity e, ref InputElement inputElement) =>
                {

                    inputElement.AttachedToLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                                        {
                                            e.Set(new AttachedToLogicalTree(sender, args));
                                            e.Emit<AttachedToLogicalTree>();
                                        };

                    inputElement.AttachedToVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new AttachedToVisualTree(sender, args));
                        e.Emit<AttachedToVisualTree>();
                    };

                    inputElement.DataContextChanged += (object sender, EventArgs args) =>
                    {
                        e.Set(new DataContextChanged(sender, args));
                        e.Emit<DataContextChanged>();
                    };

                    inputElement.DetachedFromLogicalTree += (object sender, LogicalTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromLogicalTree(sender, args));
                        e.Emit<DetachedFromLogicalTree>();
                    };

                    inputElement.DetachedFromVisualTree += (object sender, VisualTreeAttachmentEventArgs args) =>
                    {
                        e.Set(new DetachedFromVisualTree(sender, args));
                        e.Emit<DetachedFromVisualTree>();
                    };

                    inputElement.DoubleTapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new DoubleTapped(sender, args));
                        e.Emit<DoubleTapped>();
                    };

                    inputElement.EffectiveViewportChanged += (object sender, EffectiveViewportChangedEventArgs args) =>
                    {
                        e.Set(new EffectiveViewportChanged(sender, args));
                        e.Emit<EffectiveViewportChanged>();
                    };

                    inputElement.GotFocus += (object sender, GotFocusEventArgs args) =>
                    {
                        e.Set(new GotFocus(sender, args));
                        e.Emit<GotFocus>();
                    };

                    inputElement.Initialized += (object sender, EventArgs args) =>
                    {
                        e.Set(new Initialized(sender, args));
                        e.Emit<Initialized>();
                    };

                    inputElement.KeyDown += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyDown(sender, args));
                        e.Emit<KeyDown>();
                    };

                    inputElement.KeyUp += (object sender, KeyEventArgs args) =>
                    {
                        e.Set(new KeyUp(sender, args));
                        e.Emit<KeyUp>();
                    };

                    inputElement.LayoutUpdated += (object sender, EventArgs args) =>
                    {
                        e.Set(new LayoutUpdated(sender, args));
                        e.Emit<LayoutUpdated>();
                    };

                    inputElement.LostFocus += (object sender, RoutedEventArgs args) =>
                    {
                        e.Set(new LostFocus(sender, args));
                        e.Emit<LostFocus>();
                    };

                    inputElement.PointerCaptureLost += (object sender, PointerCaptureLostEventArgs args) =>
                    {
                        e.Set(new PointerCaptureLost(sender, args));
                        e.Emit<PointerCaptureLost>();
                    };

                    inputElement.PointerEntered += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerEnter(sender, args));
                        e.Emit<PointerEnter>();
                    };

                    inputElement.PointerExited += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerLeave(sender, args));
                        e.Emit<PointerLeave>();
                    };

                    inputElement.PointerMoved += (object sender, PointerEventArgs args) =>
                    {
                        e.Set(new PointerMoved(sender, args));
                        e.Emit<PointerMoved>();
                    };

                    inputElement.PointerPressed += (object sender, PointerPressedEventArgs args) =>
                    {
                        e.Set(new PointerPressed(sender, args));
                        e.Emit<PointerPressed>();
                    };

                    inputElement.PointerReleased += (object sender, PointerReleasedEventArgs args) =>
                    {
                        e.Set(new PointerReleased(sender, args));
                        e.Emit<PointerReleased>();
                    };

                    inputElement.PointerWheelChanged += (object sender, PointerWheelEventArgs args) =>
                    {
                        e.Set(new PointerWheelChanged(sender, args));
                        e.Emit<PointerWheelChanged>();
                    };

                    inputElement.PropertyChanged += (object sender, AvaloniaPropertyChangedEventArgs args) =>
                    {
                        e.Set(new PropertyChanged(sender, args));
                        e.Emit<PropertyChanged>();
                    };

                    inputElement.ResourcesChanged += (object sender, ResourcesChangedEventArgs args) =>
                    {
                        e.Set(new ResourcesChanged(sender, args));
                        e.Emit<ResourcesChanged>();
                    };

                    inputElement.Tapped += (object sender, TappedEventArgs args) =>
                    {
                        e.Set(new Tapped(sender, args));
                        e.Emit<Tapped>();
                    };


                    inputElement.TextInput += (object sender, TextInputEventArgs args) =>
                    {
                        e.Set(new TextInput(sender, args));
                        e.Emit<TextInput>();
                    };

                    inputElement.TextInputMethodClientRequested += (object sender, TextInputMethodClientRequestedEventArgs args) =>
                    {
                        e.Set(new TextInputMethodClientRequested(sender, args));
                        e.Emit<TextInputMethodClientRequested>();
                    };

                }).OnRemove((Entity e, ref InputElement inputElement) =>
                {
                });
        }
    }
}
