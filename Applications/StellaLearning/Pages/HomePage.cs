using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Flecs.NET.Core;
using NLog;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace AyanamisTower.StellaLearning.Pages;
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
        _root = world.UI<TextBlock>((t) => t.SetText("Home"))
            .Add<Page>().Entity;
    }

    /// <summary>
    /// Create a home page and attaches it to a parent
    /// </summary>
    /// <param name="world"></param>
    /// <param name="parent"></param>
    public HomePage(World world, Entity parent)
    {
        _root = world.UI<TextBlock>((t) => t.SetText("Home"))
            .Add<Page>().Entity
            .ChildOf(parent);
    }
}
