using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// Literature Page
/// </summary>
public static class LiteraturePage
{
    /// <summary>
    /// Create the Literature Page
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var literaturePage = entities.GetEntityCreateIfNotExist("LiteraturePage")
            .Add<Page>()
            .Set(new Grid())
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");

        literaturePage.AddDefaultStyling((literaturePage) =>
        {
            if (literaturePage.Parent() != 0 &&
                literaturePage.Parent().Has<NavigationView>())
            {
                switch (literaturePage.Parent().Get<NavigationView>().DisplayMode)
                {
                    case NavigationViewDisplayMode.Minimal:
                        literaturePage.SetMargin(50, 10, 20, 20);
                        break;
                    default:
                        literaturePage.SetMargin(20, 10, 20, 20);
                        break;
                }
            }
        });

        var text = entities.GetEntityCreateIfNotExist("LiteratureText")
            .Set(new TextBlock())
            .SetText("Literature")
            .ChildOf(literaturePage);
        return literaturePage;
    }
}
