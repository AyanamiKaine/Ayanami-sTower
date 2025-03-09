using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Flecs.Util;
using NLog;
using Avalonia.Flecs.Controls;

namespace Avalonia.Flecs.StellaLearning.Pages;
/// <summary>
/// Content Queue Page
/// </summary>
public class ContentQueuePage : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create the Content Queue Page
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public ContentQueuePage(World world)
    {
        _root = world.Entity()
            .Add<Page>()
            .Set(new Grid())
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");


        var listSearch = world.Entity()
            .ChildOf(_root)
            .Set(new TextBox())
            .SetColumn(0)
            .SetRow(0)
            .SetWatermark("Search Entries");

        var totalItems = world.Entity()
            .ChildOf(_root)
            .Set(new TextBlock())
            .SetVerticalAlignment(VerticalAlignment.Center)
            .SetMargin(new Thickness(10, 0))
            .SetText("Total Items: 0")
            .SetColumn(1);

        List<string> sortItems = ["Sort By Date", "Sort By Priority", "Sort By Name"];

        var myFlyout = new Flyout()
        {
            Content = new TextBlock() { Text = "Hello World" },
            ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
        };

        var sortItemsButton = world.Entity()
            .ChildOf(_root)
            .Set(new ComboBox())
            .SetPlaceholderText("Sort Items")
            .SetColumn(2)
            .SetItemsSource(sortItems)
            .SetContextFlyout(myFlyout);

        var scrollViewer = world.Entity()
            .ChildOf(_root)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        ObservableCollection<string> contentQueueItems = [];

        var contentQueueList = world.Entity()
            .ChildOf(scrollViewer)
            .Set(new ListBox())
            .SetItemsSource(contentQueueItems)
            .SetSelectionMode(SelectionMode.Multiple);
    }

    /// <inheritdoc/>
    public void Attach(Entity entity)
    {
        _root.ChildOf(entity);
    }

    /// <inheritdoc/>
    public void Detach()
    {
        _root.Remove(Ecs.ChildOf);
    }

    /// <inheritdoc/>
    public Thickness GetMargin()
    {
        return _root.GetMargin();
    }

    /// <inheritdoc/>
    public void SetMargin(Thickness margin)
    {
        _root.SetMargin(margin);
    }
}
