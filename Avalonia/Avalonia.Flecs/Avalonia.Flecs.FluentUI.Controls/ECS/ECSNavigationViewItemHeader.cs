using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Represents the ECS module for integrating NavigationViewItemHeader with Flecs.NET.
    /// </summary>
    public class ECSNavigationViewItemHeader : IFlecsModule
    {
        /// <summary>
        /// Initializes the ECS module for NavigationViewItemHeader.
        /// </summary>
        /// <param name="world">The Flecs world.</param>
        public void InitModule(World world)
        {
            world.Module<ECSNavigationViewItemHeader>();
            world
                .Component<NavigationViewItemHeader>("NavigationViewItemHeader")
                .OnSet(
                    (Entity e, ref NavigationViewItemHeader navigationViewItemHeader) =>
                    {
                        if (!e.Has<object>())
                            e.Set<object>(navigationViewItemHeader);
                        else if (e.Get<object>().GetType() == typeof(NavigationViewItemHeader))
                            e.Set<object>(navigationViewItemHeader);
                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<NavigationViewItem>())
                        {
                            parent
                                .Get<NavigationViewItem>()
                                .MenuItems.Add(navigationViewItemHeader);
                        }
                    }
                )
                .OnRemove(
                    (Entity e, ref NavigationViewItemHeader navigationViewItemHeader) =>
                    {
                        var parent = e.Parent();
                        if (parent == 0)
                        {
                            return;
                        }
                        if (parent.Has<NavigationViewItem>())
                        {
                            parent
                                .Get<NavigationViewItem>()
                                .MenuItems.Remove(navigationViewItemHeader);
                        }
                    }
                );
        }
    }
}
