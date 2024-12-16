using Flecs.NET.Core;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data;
using Avalonia.Data.Core.ExpressionNodes;
using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using FluentAvalonia.UI.Controls;

public enum ContentType
{
  File,
  Website,
  Audio,
  Video,
  Markdown, 
  Txt,
  PDF
} 

//Content represents an item that can be consumed for later time
public class Content(string name = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", string shortDescription = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", string longDescription = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.", ContentType contentType = ContentType.File, int priority = 0)
{
    public string Name { get; set; } = name;
    public string ShortDescription { get; set; } = shortDescription;
    public string LongDescription { get; set; } = longDescription;
    public int Priority { get; set; } = priority;
    public ContentType ContentType { get; set; } = contentType;
    public DateTime AddedDate   { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Name} \"{ShortDescription}\" ({AddedDate.ToShortDateString()}) TYPE: {ContentType}";
    }
}

public class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var descriptionAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttribute?.Description ?? enumValue.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    // ... (ConvertBack implementation if needed)
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



var vaultPage = entities.GetEntityCreateIfNotExist("KnowledgeVaultPage")
    .Add<Page>()
    .Set(new Grid())
    .SetColumnDefinitions(new ColumnDefinitions("*, Auto, Auto"))
    .SetRowDefinitions(new RowDefinitions("Auto, *, Auto"));

/*
Here we are setting Default styling for the page entity.
This needs some serious refactoring because we want some clean
and easy way to add default styling to an entity component like the 
page here. 

Maybe defining an extention method for entites? that takes 
an lambda function that defines the styling for it?
*/

vaultPage.AddDefaultStyling((vaultPage) => {
    if (vaultPage.Parent() != 0 && 
        vaultPage.Parent().Has<NavigationView>())
    {
        switch (vaultPage.Parent().Get<NavigationView>().DisplayMode)
        {
            case NavigationViewDisplayMode.Minimal:
                vaultPage.SetMargin(50,10,20,20);
                break;
            default:
                vaultPage.SetMargin(20,10,20,20);
                break;        
        }
    }
});



var vaultContent = entities.GetEntityCreateIfNotExist("VaultContent")
    .ChildOf(vaultPage)
    .Set(new TextBlock())
    .SetText("VaultContent")
    .SetRow(0)
    .SetColumn(0);

var scrollViewer = entities.GetEntityCreateIfNotExist("VaultScrollViewer")
    .ChildOf(vaultPage)
    .Set(new ScrollViewer())
    .SetRow(1)
    .SetColumnSpan(3);


ObservableCollection<Content> dummyItems = [
    new ("My Document", "A text document.", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.", ContentType.Txt, 1),
    new ("HackerNews Rust Article", "Rust in Linux - Drama", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut" ,ContentType.Website, 2),
    new ("HackerNews Rust Article", "Rust in Linux - Drama", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At",ContentType.Website, 20),
    new (),
    new (),
];

var contentTemplate = new FuncDataTemplate<Content>((item, nameScope) =>
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
        Margin = new Thickness(0,0,5,0)
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


    //Description
    var descriptionTextBlock = new TextBlock
    {
        TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        Margin = new Thickness(0,0,5,0)
    };
    
    descriptionTextBlock.Bind(TextBlock.TextProperty, new Binding("ShortDescription"));
    Grid.SetColumn(descriptionTextBlock, 0);
    Grid.SetRow(descriptionTextBlock, 1);
    grid.Children.Add(descriptionTextBlock);


    //Type (ENUM)
    var typeTextBlock = new TextBlock
    {
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
    };
    typeTextBlock.Bind(TextBlock.TextProperty, new Binding("ContentType"));
    Grid.SetRow(typeTextBlock, 0);
    Grid.SetColumn(typeTextBlock, 1);
    grid.Children.Add(typeTextBlock);

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
        Text = "Priority shows the importance, it determines in which order content will be consumed."
    };

    ToolTip.SetTip(priorityTextBlock, priorityTooltipTextBlock);



    return grid;
});

var vaultItems = entities.GetEntityCreateIfNotExist("VaultListBox")
    .ChildOf(scrollViewer)
    .Set(new ListBox())
    .SetItemsSource(dummyItems)
    .SetItemTemplate(contentTemplate)
    .SetSelectionMode(SelectionMode.Single);