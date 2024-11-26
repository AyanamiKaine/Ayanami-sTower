using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Pages;

public static class SettingsPage
{
    public static Entity Create(World world)
    {
        return world.Entity("SettingPage")
            .Set(new TextBlock()
            {
                Text = "Settings",
                Margin = new Thickness(10)
            });
    }
}
