using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS.Events
{
    /// <summary>
    /// Represents the event arguments for the NavigationView's SelectionChanged event.
    /// </summary>
    /// <param name="Sender">The object that raised the event.</param>
    /// <param name="Args">The event arguments.</param>
    public record struct OnSelectionChanged(object? Sender, NavigationViewSelectionChangedEventArgs Args);
}