using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;
/// <summary>
/// Home Page
/// </summary>
public static class HomePage
{
    /// <summary>
    /// Create the Home Page
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var homePage = entities.GetEntityCreateIfNotExist("HomePage")
                    .Add<Page>()
                    .Set(new TextBlock())
                    .SetText("Home");

        return homePage;
    }
}
