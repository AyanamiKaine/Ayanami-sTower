using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls.ECS.Events;
using Avalonia.Styling;
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
            .Set(new ToggleSwitch()
            {
                Content = "Dark Mode",
                IsChecked = true,
            });

        settingsPage.Get<ToggleSwitch>().IsCheckedChanged += (sender, e) =>
        {
            var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
            SetTheme(Application.Current, isDarkMode ? "Dark" : "Light");
        };

        return settingsPage;
    }

    private static Entity CreateThemeToggleSwitch(World world, Entity childOfEntity)
    {
        var themeToggleSwitch = world.Entity("ThemeToggleSwitch")
            .Add<ToggleSwitch>()
            .ChildOf(childOfEntity)
            .Set(new ToggleSwitch()
            {
                Content = "Dark Mode",
                IsChecked = true,
            });

        themeToggleSwitch.Observe<IsCheckedChanged>((Entity e) =>
        {
            var sender = e.Get<IsCheckedChanged>().Sender;

            var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
            SetTheme(Application.Current, isDarkMode ? "Dark" : "Light");
        });

        return themeToggleSwitch;
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
