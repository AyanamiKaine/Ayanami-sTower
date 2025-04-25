using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS
{
    /// <summary>
    /// Provides extension methods for Flecs entities related to FluentAvalonia NavigationView controls.
    /// </summary>
    public static class ECSNavigationViewExtensions
    {
        /// <summary>
        /// Sets the pane title for the NavigationView associated with the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="paneTitle">The pane title to set.</param>
        /// <returns>The modified entity.</returns>
        /// <exception cref="ComponentNotFoundException">Thrown when the entity does not have a NavigationView component.</exception>
        public static Entity SetPaneTitle(this Entity entity, string paneTitle)
        {
            if (entity.Has<NavigationView>())
            {
                entity.Get<NavigationView>().PaneTitle = paneTitle;
                return entity;
            }

            throw new ComponentNotFoundException(
                entity,
                typeof(ContentControl),
                nameof(NavigationView)
            );
        }
    }
}
