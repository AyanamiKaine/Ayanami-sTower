using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Represents the ECS module for integrating NavigationView controls with Flecs.
    /// </summary>
    public class ECSNavigationView : IFlecsModule
    {
        /// <summary>
        /// Initializes the ECS module for NavigationView.
        /// </summary>
        /// <param name="world">The Flecs world.</param>
        public void InitModule(World world)
        {
            world.Module<ECSNavigationView>();
            world
                .Component<NavigationView>("NavigationView")
                .OnSet(
                    (Entity e, ref NavigationView navigationView) =>
                    {
                        if (!e.Has<object>())
                            e.Set<object>(navigationView);
                        else if (e.Get<object>().GetType() == typeof(NavigationView))
                            e.Set<object>(navigationView);

                        e.Set<HeaderedContentControl>(navigationView);
                        e.Set<ContentControl>(navigationView);
                    }
                )
                .OnRemove((Entity e, ref NavigationView _) => e.Remove<ContentControl>());
        }
    }
}
