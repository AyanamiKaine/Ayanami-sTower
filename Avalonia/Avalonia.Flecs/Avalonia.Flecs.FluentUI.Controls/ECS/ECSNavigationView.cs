using Flecs.NET.Core;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.FluentUI.Controls.ECS.Module;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    public class ECSNavigationView : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<ECSNavigationView>();
            world.Component<NavigationView>("NavigationView")
                           .OnSet((Entity e, ref NavigationView navigationView) =>
                           {
                               e.Set<object>(navigationView);
                               e.Set<ContentControl>(navigationView);

                               navigationView.BackRequested += (object? sender, NavigationViewBackRequestedEventArgs args) =>
                               {
                                   e.Set(new Events.OnBackRequested(sender, args));
                                   e.Emit<Events.OnBackRequested>();
                               };

                               navigationView.ItemCollapsed += (object? sender, NavigationViewItemCollapsedEventArgs args) =>
                               {
                                   e.Set(new Events.OnItemCollapsed(sender, args));
                                   e.Emit<Events.OnItemCollapsed>();
                               };

                               navigationView.ItemExpanding += (object? sender, NavigationViewItemExpandingEventArgs args) =>
                               {
                                   e.Set(new Events.OnItemExpanding(sender, args));
                                   e.Emit<Events.OnItemExpanding>();
                               };

                               navigationView.ItemInvoked += (object? sender, NavigationViewItemInvokedEventArgs args) =>
                               {
                                   e.Set(new Events.OnItemInvoked(sender, args));
                                   e.Emit<Events.OnItemInvoked>();
                               };

                               navigationView.PaneClosing += (NavigationView sender, NavigationViewPaneClosingEventArgs args) =>
                               {
                                   e.Set(new Events.OnPaneClosing(sender, args));
                                   e.Emit<Events.OnPaneClosing>();
                               };

                               navigationView.DisplayModeChanged += (object? sender, NavigationViewDisplayModeChangedEventArgs args) =>
                               {
                                   e.Set(new Events.OnDisplayModeChanged(sender, args));
                                   e.Emit<Events.OnDisplayModeChanged>();
                               };

                               navigationView.SelectionChanged += (object? sender, NavigationViewSelectionChangedEventArgs args) =>
                               {
                                   e.Set(new Events.OnSelectionChanged(sender, args));
                                   e.Emit<Events.OnSelectionChanged>();
                               };


                           }).OnRemove((Entity e, ref NavigationView navigationView) =>
                           {
                               e.Remove<ContentControl>();
                           });
        }
    }
}
