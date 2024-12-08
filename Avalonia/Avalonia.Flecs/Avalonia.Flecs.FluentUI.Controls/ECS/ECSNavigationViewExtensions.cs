using Avalonia.Controls;
using Flecs.NET.Core;
using Avalonia.Flecs.Controls;
using FluentAvalonia.UI.Controls;
namespace Avalonia.Flecs.FluentUI.Controls.ECS
{

    public static class ECSNavigationViewExtensions
    {
        public static Entity SetPaneTitle(this Entity entity, string paneTitle)
        {
            if (entity.Has<NavigationView>())
            {
                entity.Get<NavigationView>().PaneTitle = paneTitle;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(ContentControl), nameof(NavigationView));
        }
    }
}