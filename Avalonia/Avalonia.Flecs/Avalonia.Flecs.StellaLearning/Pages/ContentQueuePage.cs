using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flecs.NET.Core;
using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Flecs.Util;

namespace Avalonia.Flecs.StellaLearning.Pages;
public static class ContentQueuePage
{
    public static Entity Create(NamedEntities entities)
    {
        var contentQueuePage = entities.GetEntityCreateIfNotExist("ContentQueuePage")
            .Add<Page>()
            .Set(new Grid())
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");


        var listSearch = entities.GetEntityCreateIfNotExist("ListSearchContentQueue")
            .ChildOf(contentQueuePage)
            .Set(new TextBox())
            .SetColumn(0)
            .SetRow(0)
            .SetWatermark("Search Entries");

        var totalItems = entities.GetEntityCreateIfNotExist("TotalItems")
            .ChildOf(contentQueuePage)
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

        var sortItemsButton = entities.GetEntityCreateIfNotExist("SortItemsButton")
            .ChildOf(contentQueuePage)
            .Set(new ComboBox())
            .SetPlaceholderText("Sort Items")
            .SetColumn(2)
            .SetItemsSource(sortItems)
            .SetContextFlyout(myFlyout);

        var scrollViewer = entities.GetEntityCreateIfNotExist("ContentQueueScrollViewer")
            .ChildOf(contentQueuePage)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        ObservableCollection<string> contentQueueItems = [];

        var contentQueueList = entities.GetEntityCreateIfNotExist("ContentQueueList")
            .ChildOf(scrollViewer)
            .Set(new ListBox())
            .SetItemsSource(contentQueueItems)
            .SetSelectionMode(SelectionMode.Multiple);

        return contentQueuePage;
    }
}
