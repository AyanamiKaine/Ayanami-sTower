using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;
public static class HomePage
{
    public static Entity Create(NamedEntities entities)
    {
        var homePage = entities.GetEntityCreateIfNotExist("HomePage")
                    .Add<Page>()
                    .Set(new TextBlock())
                    .SetText("Home");

        return homePage;
    }
}
