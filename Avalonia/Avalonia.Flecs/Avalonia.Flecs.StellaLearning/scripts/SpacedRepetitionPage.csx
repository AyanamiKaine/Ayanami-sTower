using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Avalonia.Layout;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.Controls.ECS.Module;


public enum SpacedRepetitionState
{
    NewState = 0,
    Learning = 1,
    Review = 2,
    Relearning = 3
}

public class SpacedRepetitionItem
{
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public double Stability { get; set; } = 0;
    public double Difficulty { get; set; } = 0;
    public int Priority { get; set; } = 0;
    public int Reps { get; set; } = 0;
    public int Lapsed { get; set; } = 0;
    public DateTime LastReview { get; set; } = DateTime.UtcNow;
    public DateTime NextReview { get; set; } = DateTime.UtcNow;
    public int NumberOfTimesSeen { get; set; } = 0;
    public int ElapsedDays { get; set; } = 0;
    public int ScheduledDays { get; set; } = 0;
    public SpacedRepetitionState SpacedRepetitionState { get; set; } = SpacedRepetitionState.NewState;
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



var spacedRepetitionPage = entities.GetEntityCreateIfNotExist("SpacedRepetitionPage")
    .Add<Page>()
    .Set(new Grid())
    /*
    *: This represents a "star" column. 
    It means this column will take up as much available space as 
    possible after any fixed-size or Auto columns have been accounted for. 
    Think of it as flexible or "greedy". 
    In this case, the first column will grab most of the grid's width.

    Auto: This means the column's width will adjust automatically to 
    fit the content within it. If you place a button in this column, 
    the column will be just wide enough to accommodate the button's size.
    */
    .SetColumnDefinitions(new ColumnDefinitions("*, Auto, Auto"))
    .SetRowDefinitions(new RowDefinitions("Auto, *, Auto"));

spacedRepetitionPage.AddDefaultStyling((spacedRepetitionPage) => {
    if (spacedRepetitionPage.Parent() != 0 && 
        spacedRepetitionPage.Parent().Has<NavigationView>())
    {
        switch (spacedRepetitionPage.Parent().Get<NavigationView>().DisplayMode)
        {
            case NavigationViewDisplayMode.Minimal:
                spacedRepetitionPage.SetMargin(50,10,20,20);
                break;
            default:
                spacedRepetitionPage.SetMargin(20,10,20,20);
                break;        
        }
    }
});

var listSearchSpacedRepetition = entities.GetEntityCreateIfNotExist("ListSearchSpacedRepetition")
    .ChildOf(spacedRepetitionPage)
    .Set(new TextBox())
    .SetColumn(0)
    .SetWatermark("Search Entries");

var totalItems = entities.GetEntityCreateIfNotExist("TotalSpacedRepetitionItems")
    .ChildOf(spacedRepetitionPage)
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

var sortItemsButton = entities.GetEntityCreateIfNotExist("SpacedRepetitionSortItemsButton")
    .ChildOf(spacedRepetitionPage)
    .Set(new ComboBox())
    .SetPlaceholderText("Sort Items")
    .SetColumn(2)
    .SetItemsSource(sortItems)
    .SetContextFlyout(myFlyout);

//ToolTip.SetTip(sortItemsButton.Get<ComboBox>(), myToolTip);

/*
I believe that entites should not know the exact control type but
all other entities should only care for the base classes like
Control, Panel, ItemsControl, TemplatedControl, Etc. They should
always take the lowest common denominator.

No need to depend on things that we dont care for 
*/

var scrollViewer = entities.GetEntityCreateIfNotExist("SpacedRepetitionScrollViewer")
    .ChildOf(spacedRepetitionPage)
    .Set(new ScrollViewer())
    .SetRow(1)
    .SetColumnSpan(3);

ObservableCollection<string> dummyItems = [];
var srItems = entities.GetEntityCreateIfNotExist("SpaceRepetitionList")
    .ChildOf(scrollViewer)
    .Set(new ListBox())
    .SetItemsSource(dummyItems)
    .SetSelectionMode(SelectionMode.Multiple);

listSearchSpacedRepetition.OnTextChanged((sender, args) =>
{
    //TODO:
    //We would need to implement the correct sorting of the items
    //regarding what sort settings the user set before right now
    //they are being ingnored.

    string searchText = listSearchSpacedRepetition.Get<TextBox>().Text!.ToLower();
    var filteredItems = dummyItems.Where(item => item.ToLower().Contains(searchText));
    //srItems.Get<ListBox>().ItemsSource = new ObservableCollection<string>(filteredItems);
    srItems.SetItemsSource(new ObservableCollection<string>(filteredItems));
});

//Use MenuFlyout to create a context menu
//contextMenu is used for legacy WPF apps
var contextFlyout = entities.GetEntityCreateIfNotExist("SpacedRepetitionContextFlyout")
    .ChildOf(spacedRepetitionPage)
    .Set(new MenuFlyout());

var openMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionOpenMenuItem")
    .ChildOf(contextFlyout)
    .Set(new MenuItem())
    .SetHeader("Open")
    .OnClick((sender, args) => Console.WriteLine("Open Clicked"));

var editMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionEditMenuItem")
    .ChildOf(contextFlyout)
    .Set(new MenuItem())
    .SetHeader("Edit")
    .OnClick((sender, args) => Console.WriteLine("Edit Clicked"));

var deleteMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionDeleteMenuItem")
    .ChildOf(contextFlyout)
    .Set(new MenuItem())
    .SetHeader("Delete")
    .OnClick((sender, args) => Console.WriteLine("Delete Clicked"));

dummyItems.Add("algorithm");
dummyItems.Add("binary");
dummyItems.Add("complexity");
dummyItems.Add("data structure");
dummyItems.Add("efficiency");
dummyItems.Add("Fibonacci");
dummyItems.Add("graph");
dummyItems.Add("hash table");
dummyItems.Add("iteration");
dummyItems.Add("JavaScript");

_ = sortItemsButton.OnSelectionChanged((sender, args) =>
{
    if (args.AddedItems.Count == 0)
    {
        return;
    }
    var selectedItem = args.AddedItems[0]!.ToString();
    if (selectedItem == "Sort By Date")
    {
    }
    else if (selectedItem == "Sort By Priority")
    {
        var t = (ObservableCollection<string>)srItems.GetItemsSource()!;
        t = [.. t!.OrderByDescending(s => s)];
        srItems.SetItemsSource(t);
    }
    else if (selectedItem == "Sort By Name")
    {
        //(ascending order)
        Random rng = new();
        var t = (ObservableCollection<string>)srItems.GetItemsSource()!;
        t = [.. t!.OrderBy(_ => rng.Next())];
        srItems.SetItemsSource(t);
    }
});
