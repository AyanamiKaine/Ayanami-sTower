using Avalonia.Controls;
using Flecs.NET.Core;

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
                Margin = new Thickness(10)
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
        return world.Entity("SettingPage")
            .Add<Page>()
            .ChildOf(childOfEntity)
            .Set(new TextBlock()
            {
                Text = "Settings",
                Margin = new Thickness(10)
            });
    }
}
