using Avalonia.Flecs.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using FluentAvalonia.UI.Controls;

public static class UIBuilderExtensions
{
    /// <summary>
    /// Sets an event callback for when the display mode of a navigation view changes.
    /// </summary>
    public static UIBuilder<NavigationView> OnDisplayModeChanged(this UIBuilder<NavigationView> builder, Action<object?, NavigationViewDisplayModeChangedEventArgs> handler)
    {
        builder.Entity.OnDisplayModeChanged(handler);
        return builder;
    }
}