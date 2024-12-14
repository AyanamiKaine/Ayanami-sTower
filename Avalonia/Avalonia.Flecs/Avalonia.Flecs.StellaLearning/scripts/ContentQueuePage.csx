// Problem: There is way to much to be consumed, read, heared, seen.
// Sometimes we see something that intrests us be we are unable 
// to consume it at that time. This could be an article that is quite long
// a chapter in a book(Incremental Reading), a video, a podcast, a course, a tutorial. 
// The form of the content will be vastly different so simple bookmarks
// are not enough. We need a way to queue content to be consumed later.
// Thats not enough we need to be able to queue content in different ways.
// General Places to queue content in order of importance: Files(Desktop), Browser(Addon), Files(Mobile).

// General Idea: Content Page
// We want to be able to queue different types of content to be shown in a list and incrementally
// consume it.

// Solution:
// We need to be able to show a list of content that we can consume incrementally.
// We need a way to queue content in different ways.
// We need a way to display the content in a way that is easy to consume. 
// (That could be opening the browser, opening a file, opening a video player)
// We need a way to mark content as consumed.
// We need a way to mark content as important.
// We need a way to mark content as not important.
// We need a way to tag content.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Flecs.NET.Core;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;


public class ContentQueueItem
{
    public ContentQueueItem()
    { }
}

/// <summary>
/// We can refrence the ecs world via _world its globally available in all scripts
/// we assing world = _world so the language server knows the world exists and
/// can provide us with autocompletion and correct showcase of possible compile errors
/// </summary>
public World world = _world;
/// <summary>
/// We can refrence the named entities via _entities its globally available in all scripts
/// we assing entities = _entities so the language server knows the named entities exists and
/// can provide us with autocompletion and correct showcase of possible compile errors
/// </summary>
public NamedEntities entities = _entities;

var contentQueuePage = entities.GetEntityCreateIfNotExist("ContentQueuePage")
    .Add<Page>()
    .Set(new Grid())
    .SetColumnDefinitions(new ColumnDefinitions("*, Auto, Auto"))
    .SetRowDefinitions(new RowDefinitions("Auto, *, Auto"));


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