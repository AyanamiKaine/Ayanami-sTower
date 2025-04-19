using Avalonia.Flecs.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls;

/// <summary>
/// Provides extension methods for the UIBuilder class specific to FluentAvalonia controls.
/// </summary>
public static class UIBuilderExtensions
{
    /// <summary>
    /// Sets the pane title for the NavigationView.
    /// </summary>
    /// <param name="builder">The UI builder for the NavigationView.</param>
    /// <param name="PaneTitle">The title to set for the pane.</param>
    /// <returns>The UI builder instance.</returns>
    public static UIBuilder<NavigationView> SetPaneTitle(this UIBuilder<NavigationView> builder, string PaneTitle)
    {
        builder.Entity.SetPaneTitle(PaneTitle);
        return builder;
    }

    /// <summary>
    /// Sets an event callback for when the display mode of a navigation view changes.
    /// </summary>
    public static UIBuilder<NavigationView> OnDisplayModeChanged(this UIBuilder<NavigationView> builder, Action<object?, NavigationViewDisplayModeChangedEventArgs> handler)
    {
        builder.Entity.OnDisplayModeChanged(handler);
        return builder;
    }
    /// <summary>
    /// Sets an event callback for when the selection in a navigation view changes.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static UIBuilder<NavigationView> OnNavViewSelectionChanged(this UIBuilder<NavigationView> builder, Action<object?, NavigationViewSelectionChangedEventArgs> handler)
    {
        builder.Entity.OnNavViewSelectionChanged(handler);
        return builder;
    }

    /// <summary>
    /// Sets the icon source for the NavigationViewItem.
    /// </summary>
    /// <param name="builder">The UI builder for the NavigationViewItem.</param>
    /// <param name="iconSource">The icon source to set.</param>
    /// <returns>The UI builder instance.</returns>
    public static UIBuilder<NavigationViewItem> SetIconSource(this UIBuilder<NavigationViewItem> builder, IconSource iconSource)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<NavigationViewItem>().IconSource = iconSource;

        return builder;
    }
}