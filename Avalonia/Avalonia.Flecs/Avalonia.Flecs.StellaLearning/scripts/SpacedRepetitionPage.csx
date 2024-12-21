// By default script directives (#r) are being removed
// from the script before compilation. We are just doing this here 
// so the C# Devkit and Omnisharp (For what every reason the libraries.rsp 
// does not get used any more, new bug?) can provide us with autocompletion and analysis of the code
#r "../bin/Debug/net9.0/Avalonia.Base.dll"
#r "../bin/Debug/net9.0/Avalonia.FreeDesktop.dll"
#r "../bin/Debug/net9.0/Avalonia.dll"
#r "../bin/Debug/net9.0/Avalonia.Desktop.dll"
#r "../bin/Debug/net9.0/Avalonia.X11.dll"
#r "../bin/Debug/net9.0/FluentAvalonia.dll"
#r "../bin/Debug/net9.0/Avalonia.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Markup.Xaml.dll"
#r "../bin/Debug/net9.0/Flecs.NET.dll"
#r "../bin/Debug/net9.0/Flecs.NET.Bindings.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Controls.xml"
#r "../bin/Debug/net9.0/Avalonia.Flecs.FluentUI.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.StellaLearning.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Scripting.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Scripting.xml"
#r "../bin/Debug/net9.0/FSRSPythonBridge.xml"
#r "../bin/Debug/net9.0/FSRSPythonBridge.dll"

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Scripting;
using Avalonia.Layout;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Data;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Avalonia.Data.Converters;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using FSRSPythonBridge;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.StellaLearning.Data;
public class NextReviewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return $"Next Review: {dateTime}"; // Customize date format as needed
        }
        return "Next Review: N/A"; // Handle null or incorrect types
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
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
    .SetColumnDefinitions("*, Auto, Auto")
    .SetRowDefinitions("Auto, *, Auto");

spacedRepetitionPage.AddDefaultStyling((spacedRepetitionPage) =>
{
    if (spacedRepetitionPage.Parent() != 0 &&
        spacedRepetitionPage.Parent().Has<NavigationView>())
    {
        switch (spacedRepetitionPage.Parent().Get<NavigationView>().DisplayMode)
        {
            case NavigationViewDisplayMode.Minimal:
                spacedRepetitionPage.SetMargin(50, 10, 20, 20);
                break;
            default:
                spacedRepetitionPage.SetMargin(20, 10, 20, 20);
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

List<string> itemTypes = ["File", "Quiz", "Cloze"];

var stackPanel = new StackPanel
{
    Orientation = Orientation.Vertical,
    Spacing = 5,
};
stackPanel.Children.Add(new Button { Content = "File", FontWeight = FontWeight.Bold });
stackPanel.Children.Add(new Button { Content = "Quiz", FontWeight = FontWeight.Bold });
stackPanel.Children.Add(new Button { Content = "Cloze", FontWeight = FontWeight.Bold });


var addItemsFlyout = new Flyout()
{
    Content = stackPanel,
    ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
};

var addItemButton = entities.GetEntityCreateIfNotExist("AddSpacedRepetitionItemButton")
    .ChildOf(spacedRepetitionPage)
    .Set(new Button() { Flyout = addItemsFlyout })
    .SetMargin(0, 20, 0, 0)
    .SetHorizontalAlignment(HorizontalAlignment.Right)
    .SetContent(new TextBlock() { Text = "+", FontWeight = FontWeight.Bold, FontSize = 16 })
    .SetColumn(2)
    .SetRow(2);

var scrollViewer = entities.GetEntityCreateIfNotExist("SpacedRepetitionScrollViewer")
    .ChildOf(spacedRepetitionPage)
    .Set(new ScrollViewer())
    .SetRow(1)
    .SetColumnSpan(3);

ObservableCollection<SpacedRepetitionItem> dummyItems = [];

foreach (var i in Enumerable.Range(1, 100))
{
    dummyItems.Add(new());
}


var spacedRepetitionTemplate = new FuncDataTemplate<SpacedRepetitionItem>((item, nameScope) =>
{
    var grid = new Grid
    {
        ColumnDefinitions = new ColumnDefinitions("*, *"), // Name, Description, Type
        RowDefinitions = new RowDefinitions("Auto, Auto"),
        Margin = new Thickness(0, 5)
    };


    // *** Create a TextBlock for the multi-line tooltip ***
    var tooltipTextBlock = new TextBlock
    {
        FontWeight = FontWeight.Normal,
        TextWrapping = TextWrapping.Wrap, // Enable text wrapping
        MaxWidth = 200, // Set a maximum width for wrapping
        Text = "This is a very long tooltip text that spans multiple lines. " +
                "It provides more detailed information about the content item. " +
                "You can even add more and more text to make it even longer."
    };



    //Name
    var nameTextBlock = new TextBlock
    {
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        FontWeight = FontWeight.Bold,
        Margin = new Thickness(0, 0, 5, 0)
    };
    nameTextBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
    Grid.SetColumn(nameTextBlock, 0);
    grid.Children.Add(nameTextBlock);

    /*
    For now only when we hover over the name the long description is shown
    what we want is that it is also shown when we hover over the short description

    To do this we can easily use a stack panel on which we add the name and short description
    that extends to two rows and on that stack panel then we attach the tooltip.
    */
    //ToolTip.SetTip(nameTextBlock, tooltipTextBlock);
    //tooltipTextBlock.Bind(TextBlock.TextProperty, new Binding("LongDescription"));


    //Type (ENUM)
    var typeTextBlock = new TextBlock
    {
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        Margin = new Thickness(0, 0, 5, 0)
    };

    typeTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.SpacedRepetitionItemType)));
    Grid.SetColumn(typeTextBlock, 0);
    Grid.SetRow(typeTextBlock, 1);
    grid.Children.Add(typeTextBlock);


    var nextReviewTextBlock = new TextBlock
    {
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
    };

    nextReviewTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.NextReview)));
    Grid.SetRow(nextReviewTextBlock, 0);
    Grid.SetColumn(nextReviewTextBlock, 1);
    grid.Children.Add(nextReviewTextBlock);

    //Priority
    var priorityTextBlock = new TextBlock
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
    };
    priorityTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.Priority)) { StringFormat = "Priority: {0}" });
    Grid.SetRow(priorityTextBlock, 1);
    Grid.SetColumn(priorityTextBlock, 1);
    grid.Children.Add(priorityTextBlock);



    // *** Create a TextBlock for the multi-line tooltip ***
    var priorityTooltipTextBlock = new TextBlock
    {
        FontWeight = FontWeight.Normal,
        TextWrapping = TextWrapping.Wrap, // Enable text wrapping
        MaxWidth = 200, // Set a maximum width for wrapping
        Text = "Priority shows the importance, it determines in which order items will be learned."
    };

    ToolTip.SetTip(priorityTextBlock, priorityTooltipTextBlock);

    return grid;
});


var srItems = entities.GetEntityCreateIfNotExist("SpaceRepetitionList")
    .ChildOf(scrollViewer)
    .Set(new ListBox())
    .SetItemsSource(dummyItems)
    .SetItemTemplate(spacedRepetitionTemplate)
    .SetSelectionMode(SelectionMode.Single);

listSearchSpacedRepetition.OnTextChanged((sender, args) =>
{
    //TODO:
    //We would need to implement the correct sorting of the items
    //regarding what sort settings the user set before right now
    //they are being ingnored.

    //string searchText = listSearchSpacedRepetition.Get<TextBox>().Text!.ToLower();
    //var filteredItems = dummyItems.Where(item => item.ToLower().Contains(searchText));
    //srItems.Get<ListBox>().ItemsSource = new ObservableCollection<string>(filteredItems);
    //srItems.SetItemsSource(new ObservableCollection<string>(filteredItems));
});

//Use MenuFlyout to create a context menu
//contextMenu is used for legacy WPF apps
var contextFlyout = entities.GetEntityCreateIfNotExist("SpacedRepetitionContextFlyout")
    .ChildOf(srItems)
    .Set(new MenuFlyout());

var openMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionOpenMenuItem")
    .ChildOf(contextFlyout)
    .Set(new MenuItem())
    .SetHeader("Open")
    .OnClick((sender, args) =>
    {
        var item = srItems.GetSelectedItem<SpacedRepetitionItem>();
        item.GoodReview();
        //var items = (ObservableCollection<SpacedRepetitionItem>)srItems.GetItemsSource();
        //srItems.SetItemsSource(null);
        //srItems.SetItemsSource(items);
        Console.WriteLine("Open Clicked");
        try
        {
            FileOpener.OpenMarkdownFileWithObsidian("""C:\Users\ayanami\AllTheKnowledgeReloaded\Checking if the wholeness of one center is helping another (20240518010401).md""", """C:\Users\ayanami\AppData\Local\Programs\obsidian\Obsidian.exe""");
            FileOpener.OpenFileWithDefaultProgram("""C:\Users\ayanami\nimsuggest.log""");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine(ex.Message, ex.FileName);
        }
    });

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

_ = sortItemsButton.OnSelectionChanged((sender, args) =>
{
    /*
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
    */
});
