using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input.TextInput;
using System.ComponentModel;
namespace Avalonia.Flecs.Controls.ECS.Events
{


    /* IMPORTANT
    ALL OBERSERVES RUN IN A NON-UI THREAD THIS IS THE DEFAULT BEHAVIOR IN AVALONIA
    ANY CODE EXECUTED IN AN OBSERVE THAT MODIFIES THE UI MUST BE DISPATCHED TO THE UI THREAD
    THIS CAN BE DONE BY USING THE 
    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {  UI CODE HERE  });
    THIS ALSO MATTER FOR ALL FUNCTIONS 
    THAT WANT TO USE THE ECS WORLD FOUND IN MAIN THE APPLICATION
    */

    /// <summary>
    /// Represents a button click event in the ECS system.
    /// Contains the original Avalonia event data.
    /// </summary>
    /// <param name="Sender">The object? that triggered the event</param>
    /// <param name="Args">The event arguments from Avalonia</param>
    public record struct Click(object? Sender, RoutedEventArgs Args);

    /// <summary>
    /// Represents a Checked event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Checked(object? Sender, RoutedEventArgs Args);
    /// <summary>
    /// Represents a Unchecked event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Unchecked(object? Sender, RoutedEventArgs Args);
    /// <summary>
    /// Represents a Indeterminate event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Indeterminate(object? Sender, RoutedEventArgs Args);
    /// <summary>
    /// Represents a IsCheckedChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct IsCheckedChanged(object? Sender, RoutedEventArgs Args);

    /// <summary>
    /// Represents a SelectionChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct SelectionChanged(object? Sender, SelectionChangedEventArgs Args);

    /// <summary>
    /// Represents a Activated event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Activated(object? Sender, EventArgs Args);


    /// <summary>
    /// Represents a AttachedToLogicalTree event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct AttachedToLogicalTree(object? Sender, LogicalTreeAttachmentEventArgs Args);

    /// <summary>
    /// Represents a AttachedToVisualTree event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct AttachedToVisualTree(object? Sender, VisualTreeAttachmentEventArgs Args);

    /// <summary>
    /// Represents a Closed event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Closed(object? Sender, EventArgs Args);

    /// <summary>
    /// Represents a Closing event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Closing(object? Sender, CancelEventArgs Args);

    /// <summary>
    /// Represents a DataContextChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="E"></param>
    public record struct DataContextChanged(object? Sender, EventArgs E);

    /// <summary>
    /// Represents a Deactivated event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Deactivated(object? Sender, EventArgs Args);

    /// <summary>
    /// Represents a DetachedFromLogicalTree event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct DetachedFromLogicalTree(object? Sender, LogicalTreeAttachmentEventArgs Args);

    /// <summary>
    /// Represents a DetachedFromVisualTree event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct DetachedFromVisualTree(object? Sender, VisualTreeAttachmentEventArgs Args);

    /// <summary>
    /// Represents a DoubleTapped event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct DoubleTapped(object? Sender, TappedEventArgs Args);

    /// <summary>
    /// Represents a EffectiveViewportChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct EffectiveViewportChanged(object? Sender, EffectiveViewportChangedEventArgs Args);

    /// <summary>
    /// Represents a GotFocus event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct GotFocus(object? Sender, GotFocusEventArgs Args);

    /// <summary>
    /// Represents a Initialized event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Initialized(object? Sender, EventArgs Args);

    /// <summary>
    /// Represents a KeyDown event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct KeyDown(object? Sender, KeyEventArgs Args);

    /// <summary>
    /// Represents a KeyUp event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct KeyUp(object? Sender, KeyEventArgs Args);

    /// <summary>
    /// Represents a LayoutUpdated event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct LayoutUpdated(object? Sender, EventArgs Args);

    /// <summary>
    /// Represents a LostFocus event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct LostFocus(object? Sender, RoutedEventArgs Args);

    /// <summary>
    /// Represents a Opened event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Opened(object? Sender, EventArgs Args);

    /// <summary>
    /// Represents a PointerCaptureLost event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerCaptureLost(object? Sender, PointerCaptureLostEventArgs Args);

    /// <summary>
    /// Represents a PointerEnter event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerEnter(object? Sender, PointerEventArgs Args);

    /// <summary>
    /// Represents a PointerLeave event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerLeave(object? Sender, PointerEventArgs Args);

    /// <summary>
    /// Represents a PointerMoved event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerMoved(object? Sender, PointerEventArgs Args);

    /// <summary>
    /// Represents a PointerPressed event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerPressed(object? Sender, PointerPressedEventArgs Args);

    /// <summary>
    /// Represents a PointerReleased event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerReleased(object? Sender, PointerReleasedEventArgs Args);

    /// <summary>
    /// Represents a PointerWheelChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PointerWheelChanged(object? Sender, PointerWheelEventArgs Args);

    /// <summary>
    /// Represents a PositionChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PositionChanged(object? Sender, PixelPointEventArgs Args);

    /// <summary>
    /// Represents a PropertyChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct PropertyChanged(object? Sender, AvaloniaPropertyChangedEventArgs Args);

    /// <summary>
    /// Represents a ResourcesChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct ResourcesChanged(object? Sender, ResourcesChangedEventArgs Args);

    /// <summary>
    /// Represents a Tapped event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct Tapped(object? Sender, TappedEventArgs Args);

    /// <summary>
    /// Represents a TemplateApplied event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct TemplateApplied(object? Sender, TemplateAppliedEventArgs Args);

    /// <summary>
    /// Represents a TextInput event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct TextInput(object? Sender, TextInputEventArgs Args);

    /// <summary>
    /// Represents a TextChanging event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct TextChanging(object? Sender, TextChangingEventArgs Args);

    /// <summary>
    /// Represents a TextChanged event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct TextChanged(object? Sender, TextChangedEventArgs Args);

    /// <summary>
    /// Represents a TextInputMethodClientRequested event in the ECS system.
    /// </summary>
    /// <param name="Sender"></param>
    /// <param name="Args"></param>
    public record struct TextInputMethodClientRequested(object? Sender, TextInputMethodClientRequestedEventArgs Args);
}