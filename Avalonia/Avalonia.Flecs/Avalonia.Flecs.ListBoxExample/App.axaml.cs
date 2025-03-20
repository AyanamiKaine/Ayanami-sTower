using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.ListBoxExample;

public class MyItem
{
    public DateTime Date { get; set; }
    public required string Name { get; set; }
    public required string FilePath { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Date.ToShortDateString()})"; // Default display if no template
    }
}


public partial class App : Application
{
    private World _world = World.Create();
    private NamedEntities? _entities;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _world.Import<Controls.ECS.Module>();
        _world.Import<FluentUI.Controls.ECS.Module>();
        _entities = new NamedEntities(_world);

        var window = _entities.Create("MainWindow")
            .Set(new Window())
            .SetWindowTitle("Stella Learning")
            .SetHeight(400)
            .SetWidth(400);

        CreateListBoxComponent(_world)
            .ChildOf(window);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && _entities is not null)
        {
            desktop.MainWindow = _entities["MainWindow"].Get<Window>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// This accesses the source items in the listBox and
    /// changes the Name property of each item. This change
    /// should be automatically be reflected in the UI.
    /// Because we are using data binding.
    /// </summary>
    /// <param name="listBox"></param>
    public static void ChangingTheSourceItems(Entity listBox)
    {
        var items = listBox.GetItemsSource();
        if (items != null) // Check for null if needed
        {
            foreach (MyItem item in items) // No cast needed!
            {
                // 'item' is directly of type MyItem
                Console.WriteLine(item.Name); // Access properties directly
                item.Name = "New Name"; // Modify properties directly
            }
        }
    }

    /// <summary>
    /// This shows how to create a ListBox with a custom ItemTemplate
    /// using Flecs.NET.
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public Entity CreateListBoxComponent(World world)
    {
        var listBox = _entities!.Create("ListBox")
            .Set(new ListBox());

        var items = new ObservableCollection<MyItem>
        {
            new() { Date = DateTime.Now, Name = "Item 1", FilePath = "/path/to/file1" },
            new() { Date = DateTime.Now.AddDays(-1), Name = "Item 2", FilePath = "/path/to/file2" },
            new() { Date = DateTime.Now.AddDays(-2), Name = "Item 3", FilePath = "/path/to/file3" }
        };


        /*
        item is the actual data object being displayed. 
        You use it to access the data you want to show.

        nameScope is for managing named elements within the template, 
        which is typically not needed for basic templating.
        */
        var template = new FuncDataTemplate<MyItem>((item, nameScope) =>
        {
            // Create the visual elements for each item
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var dateTextBlock = new TextBlock
            {
                // Format the date
                Margin = new Thickness(5),
                Width = 80 // Set a fixed width for better alignment
            };
            dateTextBlock.Bind(TextBlock.TextProperty, new Binding("Date") { Converter = new DateToStringConverter(), ConverterParameter = "yyyy-MM-dd" });

            var nameTextBlock = new TextBlock
            {
                Margin = new Thickness(5),
                Width = 150
            };
            nameTextBlock.Bind(TextBlock.TextProperty, new Binding("Name"));

            var filePathTextBlock = new TextBlock
            {
                Margin = new Thickness(5),
                TextTrimming = TextTrimming.CharacterEllipsis, // Truncate long paths
                Width = 200
            };
            filePathTextBlock.Bind(TextBlock.TextProperty, new Binding("FilePath"));

            stackPanel.Children.Add(dateTextBlock);
            stackPanel.Children.Add(nameTextBlock);
            stackPanel.Children.Add(filePathTextBlock);

            return stackPanel;
        });

        listBox
            .SetItemsSource(items)
            .SetItemTemplate(template);


        return listBox;
    }


    /// <summary>
    /// This shows how to create a ListBox with a custom ItemTemplate
    /// using plain C# code.
    /// </summary>
    /// <returns></returns>
    public static Control CreateListBox()
    {
        // Create a ListBox
        var listBox = new ListBox();

        // Create an ObservableCollection to hold your items
        var items = new ObservableCollection<MyItem>
        {
            new() { Date = DateTime.Now, Name = "Item 1", FilePath = "/path/to/file1" },
            new() { Date = DateTime.Now.AddDays(-1), Name = "Item 2", FilePath = "/path/to/file2" },
            new() { Date = DateTime.Now.AddDays(-2), Name = "Item 3", FilePath = "/path/to/file3" }
        };

        // Set the ListBox's ItemsSource
        listBox.ItemsSource = items;

        // *** Create the ItemTemplate ***
        /*
        item is the actual data object being displayed. 
        You use it to access the data you want to show.

        nameScope is for managing named elements within the template, 
        which is typically not needed for basic templating.
        */
        listBox.ItemTemplate = new FuncDataTemplate<MyItem>((item, nameScope) =>
        {
            // Create the visual elements for each item
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var dateTextBlock = new TextBlock
            {
                // Format the date
                Margin = new Thickness(5),
                Width = 80 // Set a fixed width for better alignment
            };
            dateTextBlock.Bind(TextBlock.TextProperty, new Binding("Date") { Converter = new DateToStringConverter(), ConverterParameter = "yyyy-MM-dd" });

            var nameTextBlock = new TextBlock
            {
                Margin = new Thickness(5),
                Width = 150
            };
            nameTextBlock.Bind(TextBlock.TextProperty, new Binding("Name"));

            var filePathTextBlock = new TextBlock
            {
                /*
                If you use Text = item.FilePath, the UI will only be set 
                once when the template is created. Any subsequent changes to 
                item.FilePath will not be reflected in the UI.
                */
                //Text = item.FilePath,
                Margin = new Thickness(5),
                TextTrimming = TextTrimming.CharacterEllipsis, // Truncate long paths
                Width = 200
            };
            /*
             If the FilePath property of your MyItem object changes after 
             it's been displayed in the ListBox, the UI will automatically
             update to reflect the new value if you're using data binding. 
             This is a core feature of data binding and is essential for dynamic UIs.
            */
            filePathTextBlock.Bind(TextBlock.TextProperty, new Binding("FilePath"));

            stackPanel.Children.Add(dateTextBlock);
            stackPanel.Children.Add(nameTextBlock);
            stackPanel.Children.Add(filePathTextBlock);

            return stackPanel;
        });
        return listBox;
    }

    // Create a ValueConverter for Date Formatting
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString((string)parameter! ?? "yyyy-MM-dd", culture); // Use parameter for format
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException(); // Conversion back is not needed in this case
        }
    }

}