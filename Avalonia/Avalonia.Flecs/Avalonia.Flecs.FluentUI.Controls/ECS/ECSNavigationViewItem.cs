using Avalonia.Controls;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Represents a Flecs module for integrating FluentAvalonia NavigationViewItem with the ECS world.
    /// </summary>
    public class ECSNavigationViewItem : IFlecsModule
    {
        /// <summary>
        /// Initializes the module in the specified Flecs world.
        /// </summary>
        /// <param name="world">The Flecs world to initialize the module in.</param>
        public void InitModule(World world)
        {
            world.Module<ECSNavigationViewItem>();
            world.Component<NavigationViewItem>("NavigationViewItem")
                            .OnSet((Entity e, ref NavigationViewItem navigationViewItem) =>
                            {
                                if (!e.Has<object>())
                                    e.Set<object>(navigationViewItem);
                                else if (e.Get<object>().GetType() == typeof(NavigationViewItem))
                                    e.Set<object>(navigationViewItem);

                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<NavigationView>())
                                {
                                    parent.Get<NavigationView>().MenuItems.Add(navigationViewItem);
                                    parent.Get<NavigationView>().SelectedItem = navigationViewItem;
                                }
                                e.Set<ContentControl>(navigationViewItem);
                            }).OnRemove((Entity e, ref NavigationViewItem navigationViewItem) =>
                            {
                                var parent = e.Parent();
                                if (parent == 0)
                                {
                                    return;
                                }
                                if (parent.Has<NavigationView>())
                                {
                                    parent.Get<NavigationView>().MenuItems.Remove(navigationViewItem);
                                }
                            });
        }
    }
}
