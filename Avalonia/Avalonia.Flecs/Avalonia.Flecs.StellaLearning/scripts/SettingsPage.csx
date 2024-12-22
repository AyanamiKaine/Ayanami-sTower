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
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Flecs.NET.Core;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Flecs.Scripting;
using Avalonia.Layout;
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

var SettingsProvider = entities.GetEntityCreateIfNotExist("SettingsProvider")
    .Set(new Settings());

/// <summary>
/// Creates a new toggle switch entity that has the ability
/// to switch between dark and light mode.
/// </summary>
/// <param name="world"></param>
/// <param name="childOfEntity"></param>
/// <returns></returns>
private Entity ThemeToggleSwitch(World world, Entity childOfEntity)
{
    var themeToggleSwitch = entities.GetEntityCreateIfNotExist("ThemeToggleSwitch")
        .ChildOf(childOfEntity)
        .Set(new ToggleSwitch())
        .SetContent("Dark Mode")
        .OnIsCheckedChanged((sender, args) =>
        {
            var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
            if (Application.Current is not null)
                SetTheme(Application.Current, isDarkMode ? "Dark" : "Light");
        });

    return themeToggleSwitch;
}

private Entity ObsidianPath(World world, Entity childOfEntity)
{
    var browseForObsidianButton = entities.GetEntityCreateIfNotExist("BrowseForObsidianButton")
        .Set(new Button());

    var browseForObsidianButtonContent = entities.GetEntityCreateIfNotExist("BrowseForObsidianButtonContent")
        .ChildOf(browseForObsidianButton)
        .Set(new TextBlock())
        .SetText("Browse");


    var toolTipTextBlock = entities.GetEntityCreateIfNotExist("ToolTipTextBlock")
        .Set(new TextBlock())
        .SetText(
            """
                When a obsidian path is set, the application 
                will be able to open the obsidian vault when a
                markdown file is part of an obsidian vault.
                """);

    var obsidianPathTooltip = entities.GetEntityCreateIfNotExist("ObsidianPathTooltip")
        .Set(new ToolTip())
        .SetContent(toolTipTextBlock);

    var obsidianPath = entities.GetEntityCreateIfNotExist("ObsidianPath")
        .ChildOf(childOfEntity)
        .Set(new TextBox())
        .SetText(SettingsProvider.Get<Settings>().ObsidianPath)
        .SetWatermark("Path to Obsidian")
        .SetInnerRightContent(browseForObsidianButton)
        .AttachToolTip(obsidianPathTooltip);

    browseForObsidianButton
        .ChildOf(obsidianPath)
        .OnClick(async (e, args) => 
        {
            string newObsidanPath = await ObsidianFilePickerAsync(world);
            if (newObsidanPath != "" && obsidianPath.GetText() == "")    
                obsidianPath.SetText(newObsidanPath);
            SettingsProvider.Get<Settings>().ObsidianPath = obsidianPath.GetText();
            Console.WriteLine("New Obsidian Path:" + SettingsProvider.Get<Settings>().ObsidianPath);
        });

    return obsidianPath;
}

/* Unmerged change from project 'Avalonia.Flecs.StellaLearning (net9.0)'
Before:
    private static string ObsidianFilePicker(World world)
After:
    private static string ObsidianFilePickerAsync(World world)
*/
private static async Task<string> ObsidianFilePickerAsync(World world)
{
    // Create and configure the file picker options
    var options = new FilePickerOpenOptions
    {
        Title = "Select the obsidian executable",
        AllowMultiple = false, // Set to true if you want to allow multiple file selections
    };

    // Create an OpenFileDialog instance
    IReadOnlyList<IStorageFile> result = await world.Lookup("MainWindow").Get<Window>().StorageProvider.OpenFilePickerAsync(options);

    if (result != null && result?.Count > 0)
    {
        // Get the selected file
        IStorageFile file = result[0];

        // Do something with the file path, e.g., display it in a TextBox
        return file.Path.AbsolutePath;
    }
    return "";
}

private static void SetTheme(Application app, string theme)
{
    // Get the current app theme variant
    var currentThemeVariant = app.ActualThemeVariant;

    // Determine the new theme variant based on the input string

    var newThemeVariant = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? ThemeVariant.Dark : ThemeVariant.Light;

    // Only update the theme if it has changed
    if (currentThemeVariant != newThemeVariant)
    {
        app.RequestedThemeVariant = newThemeVariant;
    }
}

var settingsPage = entities.GetEntityCreateIfNotExist("SettingsPage")
    .Add<Page>()
    .Set(new StackPanel());

ThemeToggleSwitch(world, settingsPage);
ObsidianPath(world, settingsPage);
