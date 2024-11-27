using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input.TextInput;
namespace Avalonia.Flecs.Controls.ECS.Events
{
    /////EVENTS

    /// <summary>
    /// Represents a button click event in the ECS system.
    /// Contains the original Avalonia event data.
    /// </summary>
    /// <param name="Sender">The object that triggered the event</param>
    /// <param name="args">The event arguments from Avalonia</param>
    public record struct Click(object Sender, RoutedEventArgs Args);
    public record struct Checked(object Sender, RoutedEventArgs Args);
    public record struct Unchecked(object Sender, RoutedEventArgs Args);
    public record struct Indeterminate(object Sender, RoutedEventArgs Args);
    public record struct SelectionChanged(object Sender, SelectionChangedEventArgs Args);
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
}