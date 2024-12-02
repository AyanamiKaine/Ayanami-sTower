using Flecs.NET.Core;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Input.TextInput;
using Avalonia.Controls.Primitives;
namespace Avalonia.Flecs.Controls.ECS
{
    public class ECSWindow : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSWindow>();
            world.Component<Window>("Window")
                           .OnSet((Entity e, ref Window window) =>
                           {
                               e.Set<ContentControl>(window);

                               /// IMPORTANT
                               /// ALL OBERSERVES RUN IN A NON-UI 
                               /// THREAD THIS IS THE DEFAULT BEHAVIOR IN AVALONIA
                               /// ANY CODE EXECUTED IN AN OBSERVE 
                               /// THAT MODIFIES THE UI MUST BE DISPATCHED TO THE UI THREAD
                               /// THIS CAN BE DONE BY USING THE 
                               /// Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { /* UI CODE HERE */ });
                               /// THIS ALSO MATTER FOR ALL FUNCTIONS 
                               /// THAT WANT TO USE THE ECS WORLD FOUND IN MAIN THE APPLICATION

                               window.Activated += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new Activated(sender, args));
                                   e.Emit<Activated>();
                               };

                               window.AttachedToLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                               {
                                   e.Set(new AttachedToLogicalTree(sender, args));
                                   e.Emit<AttachedToLogicalTree>();
                               };

                               window.AttachedToVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                               {
                                   e.Set(new AttachedToVisualTree(sender, args));
                                   e.Emit<AttachedToVisualTree>();
                               };

                               window.Closed += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new Closed(sender, args));
                                   e.Emit<Closed>();
                               };

                               window.DataContextChanged += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new DataContextChanged(sender, args));
                                   e.Emit<DataContextChanged>();
                               };

                               window.Deactivated += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new Deactivated(sender, args));
                                   e.Emit<Deactivated>();
                               };

                               window.DetachedFromLogicalTree += (object? sender, LogicalTreeAttachmentEventArgs args) =>
                               {
                                   e.Set(new DetachedFromLogicalTree(sender, args));
                                   e.Emit<DetachedFromLogicalTree>();
                               };

                               window.DetachedFromVisualTree += (object? sender, VisualTreeAttachmentEventArgs args) =>
                               {
                                   e.Set(new DetachedFromVisualTree(sender, args));
                                   e.Emit<DetachedFromVisualTree>();
                               };

                               window.DoubleTapped += (object? sender, TappedEventArgs args) =>
                               {
                                   e.Set(new DoubleTapped(sender, args));
                                   e.Emit<DoubleTapped>();
                               };

                               window.EffectiveViewportChanged += (object? sender, EffectiveViewportChangedEventArgs args) =>
                               {
                                   e.Set(new EffectiveViewportChanged(sender, args));
                                   e.Emit<EffectiveViewportChanged>();
                               };

                               window.PositionChanged += (object? sender, PixelPointEventArgs args) =>
                               {
                                   e.Set(new PositionChanged(sender, args));
                                   e.Emit<PositionChanged>();
                               };

                               window.GotFocus += (object? sender, GotFocusEventArgs args) =>
                               {
                                   e.Set(new GotFocus(sender, args));
                                   e.Emit<GotFocus>();
                               };

                               window.Initialized += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new Initialized(sender, args));
                                   e.Emit<Initialized>();
                               };

                               window.KeyDown += (object? sender, KeyEventArgs args) =>
                               {
                                   e.Set(new KeyDown(sender, args));
                                   e.Emit<KeyDown>();
                               };

                               window.KeyUp += (object? sender, KeyEventArgs args) =>
                               {
                                   e.Set(new KeyUp(sender, args));
                                   e.Emit<KeyUp>();
                               };

                               window.LayoutUpdated += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new LayoutUpdated(sender, args));
                                   e.Emit<LayoutUpdated>();
                               };

                               window.LostFocus += (object? sender, RoutedEventArgs args) =>
                               {
                                   e.Set(new LostFocus(sender, args));
                                   e.Emit<LostFocus>();
                               };

                               window.PointerCaptureLost += (object? sender, PointerCaptureLostEventArgs args) =>
                               {
                                   e.Set(new PointerCaptureLost(sender, args));
                                   e.Emit<PointerCaptureLost>();
                               };

                               window.PointerEntered += (object? sender, PointerEventArgs args) =>
                               {
                                   e.Set(new PointerEnter(sender, args));
                                   e.Emit<PointerEnter>();
                               };

                               window.PointerExited += (object? sender, PointerEventArgs args) =>
                               {
                                   e.Set(new PointerLeave(sender, args));
                                   e.Emit<PointerLeave>();
                               };

                               window.PointerMoved += (object? sender, PointerEventArgs args) =>
                               {
                                   e.Set(new PointerMoved(sender, args));
                                   e.Emit<PointerMoved>();
                               };

                               window.PointerPressed += (object? sender, PointerPressedEventArgs args) =>
                               {
                                   e.Set(new PointerPressed(sender, args));
                                   e.Emit<PointerPressed>();
                               };

                               window.PointerReleased += (object? sender, PointerReleasedEventArgs args) =>
                               {
                                   e.Set(new PointerReleased(sender, args));
                                   e.Emit<PointerReleased>();
                               };

                               window.PointerWheelChanged += (object? sender, PointerWheelEventArgs args) =>
                               {
                                   e.Set(new PointerWheelChanged(sender, args));
                                   e.Emit<PointerWheelChanged>();
                               };

                               window.PropertyChanged += (object? sender, AvaloniaPropertyChangedEventArgs args) =>
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

                               window.Tapped += (object? sender, TappedEventArgs args) =>
                               {
                                   e.Set(new Tapped(sender, args));
                                   e.Emit<Tapped>();
                               };

                               window.TextInput += (object? sender, TextInputEventArgs args) =>
                               {
                                   e.Set(new TextInput(sender, args));
                                   e.Emit<TextInput>();
                               };

                               window.TextInputMethodClientRequested += (object? sender, TextInputMethodClientRequestedEventArgs args) =>
                               {
                                   e.Set(new TextInputMethodClientRequested(sender, args));
                                   e.Emit<TextInputMethodClientRequested>();
                               };

                               window.TemplateApplied += (object? sender, TemplateAppliedEventArgs args) =>
                               {
                                   e.Set(new TemplateApplied(sender, args));
                                   e.Emit<TemplateApplied>();
                               };

                               window.Opened += (object? sender, EventArgs args) =>
                               {
                                   e.Set(new Opened(sender, args));
                                   e.Emit<Opened>();
                               };
                           })
                           .OnRemove((Entity e, ref Window window) =>
                           {
                               window.Close();
                           });
        }
    }
}
