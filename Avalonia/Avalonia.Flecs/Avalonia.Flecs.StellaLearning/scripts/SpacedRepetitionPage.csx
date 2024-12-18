
// By default script directives (#r) are being removed
// from the script before compilation. We are just doing this here 
// so the C# Devkit can provide us with autocompletion and analysis of the code
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
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Data;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Avalonia.Data.Converters;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class FileOpener
{
    public static void OpenFileWithDefaultProgram(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use Process.Start with "explorer.exe" and the file path.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + filePath + "\"" // Important: Quote the path in case it contains spaces.
                });

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Linux/macOS: Use xdg-open (Linux) or open (macOS)
                string opener = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" : "xdg-open";

                Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = opener,
                        Arguments = "\"" + filePath + "\"", // Quote the path
                        UseShellExecute = false, // Required for redirection
                        CreateNoWindow = true, // Optional: Don't show a console window
                        RedirectStandardError = true // Capture error output for debugging
                    }
                };
                process.Start();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    // Handle errors (e.g., file not found, no application associated)
                    Console.WriteLine($"Error opening file: {error}");
                    throw new Exception($"Error opening file: {error}");
                }

            }
            else
            {
                // Handle other operating systems or throw an exception.
                throw new PlatformNotSupportedException("Operating system not supported.");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., file not found, no associated program).
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw; // Re-throw the exception after logging, if needed.
        }
    }
}

public enum SpacedRepetitionItemType
{
    Image,
    Video,
    Quiz,
    Flashcard,
    Text,
    Exercise,
    File,
    PDF,
    Executable,
}

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
    public string Name { get; set; } = "Lorem Ipsum";
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
    public SpacedRepetitionItemType SpacedRepetitionItemType { get; set; } = SpacedRepetitionItemType.Text;
}

public class SpacedRepetitionQuiz : SpacedRepetitionItem
{
    public string Question { get; set; } = "Lorem Ispusm";
    public List<string> Answers { get; set; } = ["Lorem Ispusm", "Lorem Ispusmiusm Dorema"];
    public int CorrectAnswerIndex { get; set; } = 0;
}

public class SpacedRepetitionFlashcard : SpacedRepetitionItem
{
    public string Front { get; set; } = "Front";
    public string Back { get; set; } = "Back";
}

public class SpacedRepetitionVideo : SpacedRepetitionItem
{
    public string VideoUrl { get; set; } = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
}

public class SpacedRepetitionFile : SpacedRepetitionItem
{
    public string FilePath { get; set; } = "C:/Users/username/Documents/MyFile.txt";
}

public class SpacedRepetitionExercise : SpacedRepetitionItem
{
    public string Problem { get; set; } = "Lorem Ipsum";
    public string Solution { get; set; } = "Lorem Ipsum";
}

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

var scrollViewer = entities.GetEntityCreateIfNotExist("SpacedRepetitionScrollViewer")
    .ChildOf(spacedRepetitionPage)
    .Set(new ScrollViewer())
    .SetRow(1)
    .SetColumnSpan(3);

ObservableCollection<SpacedRepetitionItem> dummyItems = [
    new(),
    new(),
    new(),
];


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
    ToolTip.SetTip(nameTextBlock, tooltipTextBlock);
    tooltipTextBlock.Bind(TextBlock.TextProperty, new Binding("LongDescription")); // Assuming you have a "HoverText" property in your data context


    //Type (ENUM)
    var typeTextBlock = new TextBlock
    {
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        Margin = new Thickness(0, 0, 5, 0)
    };

    typeTextBlock.Bind(TextBlock.TextProperty, new Binding("SpacedRepetitionItemType"));
    Grid.SetColumn(typeTextBlock, 0);
    Grid.SetRow(typeTextBlock, 1);
    grid.Children.Add(typeTextBlock);


    var nextReviewTextBlock = new TextBlock
    {
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
    };
    nextReviewTextBlock.Bind(TextBlock.TextProperty, new Binding("NextReview") { Converter = new NextReviewConverter() });
    Grid.SetRow(nextReviewTextBlock, 0);
    Grid.SetColumn(nextReviewTextBlock, 1);
    grid.Children.Add(nextReviewTextBlock);

    //Priority
    var priorityTextBlock = new TextBlock
    {
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
    };
    priorityTextBlock.Bind(TextBlock.TextProperty, new Binding("Priority") { StringFormat = "Priority: {0}" });
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
    .SetSelectionMode(SelectionMode.Multiple);

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
    .ChildOf(spacedRepetitionPage)
    .Set(new MenuFlyout());

var openMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionOpenMenuItem")
    .ChildOf(contextFlyout)
    .Set(new MenuItem())
    .SetHeader("Open")
    .OnClick((sender, args) =>
    {
        Console.WriteLine("Open Clicked");
        FileOpener.OpenFileWithDefaultProgram("/home/ayanami/Ayanami-sTower/Avalonia/Avalonia.Flecs/Avalonia.Flecs.StellaLearning/bin/Debug/net9.0/Avalonia.Flecs.Scripting.xml");
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
