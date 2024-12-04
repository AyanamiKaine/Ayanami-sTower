using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;

public static class SettingsPage
{
    public static Entity Create(World world)
    {
        return world.Entity("SettingPage")
            .Add<Page>()
            .Set(new TextBlock()
            {
                Text = "Settings",
            });
    }

    /// <summary>
    /// Creates a new SettingsPage entity and sets it as a child of the provided entity.
    /// </summary>
    /// <param name="world">Flecs ECS World where the entity is added to</param>
    /// <param name="childOfEntity">parent of the created settings page</param>
    /// <returns></returns>
    public static Entity Create(World world, Entity childOfEntity)
    {
        var settingsPage = world.Entity("SettingsPage")
            .Add<Page>()
            .ChildOf(childOfEntity)
            .Set(new StackPanel());

        ThemeToggleSwitch(world, settingsPage);
        ObsidianPath(world, settingsPage);
        return settingsPage;
    }

    /// <summary>
    /// Creates a new toggle switch entity that has the ability
    /// to switch between dark and light mode.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="childOfEntity"></param>
    /// <returns></returns>
    private static Entity ThemeToggleSwitch(World world, Entity childOfEntity)
    {
        var themeToggleSwitch = world.Entity("ThemeToggleSwitch")
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

    private static Entity ObsidianPath(World world, Entity childOfEntity)
    {

        var browseForObsidianButton = world.Entity("BrowseForObsidianButton")
            .Set(new Button());

        var browseForObsidianButtonContent = world.Entity("TextBlock")
            .ChildOf(browseForObsidianButton)
            .Set(new TextBlock())
            .SetText("Browse");

        var obsidianPath = world.Entity("ObsidianPath")
            .ChildOf(childOfEntity)
            .Set(new TextBox())
            .SetWatermark("Path to Obsidian")
            .SetInnerRightContent(browseForObsidianButton.Get<Button>());

        browseForObsidianButton
            .ChildOf(obsidianPath)
            .OnClick(async (e, args) =>
            {
                obsidianPath.SetText(await ObsidianFilePickerAsync(world));
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
        IReadOnlyList<IStorageFile>? result = await world.Lookup("MainWindow").Get<Window>().StorageProvider.OpenFilePickerAsync(options);

        if (result != null && result.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            // Get the file path
            string filePath = file.Path.AbsolutePath;

            // Do something with the file path, e.g., display it in a TextBox
            return filePath;
        }
        return "";
    }

    private static void SetTheme(Application app, string theme)
    {
        // Get the current app theme variant
        var currentThemeVariant = app.ActualThemeVariant;

        // Determine the new theme variant based on the input string
        var newThemeVariant = theme.ToLower() == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        // Only update the theme if it has changed
        if (currentThemeVariant != newThemeVariant)
        {
            app.RequestedThemeVariant = newThemeVariant;
        }
    }
}
