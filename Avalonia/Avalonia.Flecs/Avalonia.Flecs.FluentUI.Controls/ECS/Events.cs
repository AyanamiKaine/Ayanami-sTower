using Avalonia.Controls;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS.Events
{
    public record struct OnSelectionChanged(object? Sender, NavigationViewSelectionChangedEventArgs Args);
    public record struct OnDisplayModeChanged(object? Sender, NavigationViewDisplayModeChangedEventArgs Args);
    public record struct OnBackRequested(object? Sender, NavigationViewBackRequestedEventArgs Args);
    public record struct OnItemCollapsed(object? Sender, NavigationViewItemCollapsedEventArgs Args);
    public record struct OnItemExpanding(object? Sender, NavigationViewItemExpandingEventArgs Args);
    public record struct OnItemInvoked(object? Sender, NavigationViewItemInvokedEventArgs Args);
    public record struct OnPaneClosing(object? Sender, NavigationViewPaneClosingEventArgs Args);

}