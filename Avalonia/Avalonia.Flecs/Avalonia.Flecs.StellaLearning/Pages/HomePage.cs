using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;
using NLog;
using Windows.UI.Popups;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;
/// <summary>
/// Home Page
/// </summary>
public class HomePage : IUIComponent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    /// <summary>
    /// Creates a home page.
    /// </summary>
    /// <param name="world"></param>
    public HomePage(World world)
    {
        _root = world.Entity()
                    .Add<Page>()
                    .Set(new TextBlock())
                    .SetText("Home");
    }

    /// <summary>
    /// Create a home page and attaches it to a parent
    /// </summary>
    /// <param name="world"></param>
    /// <param name="parent"></param>
    public HomePage(World world, Entity parent)
    {
        _root = world.Entity()
                    .Add<Page>()
                    .Set(new TextBlock())
                    .SetText("Home")
                    .ChildOf(parent);
    }
}
