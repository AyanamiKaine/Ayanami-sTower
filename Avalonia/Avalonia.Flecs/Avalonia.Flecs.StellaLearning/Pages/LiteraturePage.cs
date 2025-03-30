using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using NLog;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// Literature Page
/// </summary>
public class LiteraturePage : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create the Literature Page
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public LiteraturePage(World world)
    {
        _root = world.UI<Grid>((grid) =>
        {
            grid
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");
        }).Add<Page>().Entity;

        _root.AddDefaultStyling((literaturePage) =>
        {
            if (literaturePage.Parent() != 0 &&
                literaturePage.Parent().Has<NavigationView>())
            {
                switch (literaturePage.Parent().Get<NavigationView>().DisplayMode)
                {
                    case NavigationViewDisplayMode.Minimal:
                        //literaturePage.SetMargin(50, 10, 20, 20);
                        break;
                    default:
                        //literaturePage.SetMargin(20, 10, 20, 20);
                        break;
                }
            }
        });

        var text = world.UI<TextBlock>((textBlock) =>
        {
            textBlock.SetText("Literature");

        }).Entity.ChildOf(_root);
    }
}
