using Avalonia.Flecs.Controls;
using Avalonia.Flecs.FluentUI.Controls.ECS;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls;
public static class UIBuilderExtensions
{
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

    public static UIBuilder<NavigationViewItem> SetIconSource(this UIBuilder<NavigationViewItem> builder, IconSource iconSource)
    {
        if (!builder.Entity.IsValid() || !builder.Entity.IsAlive() || builder.Entity == 0)
            return builder;

        builder.Get<NavigationViewItem>().IconSource = iconSource;

        return builder;
    }
}