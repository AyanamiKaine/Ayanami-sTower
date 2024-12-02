using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace Avalonia.Flecs.StellaLearning.Pages;

public static class LiteraturePage
{
    public static Entity Create(World world)
    {
        //If you give an entity a name it can be later used to identify it
        //It becomes a unique entity, two entities with the same name are the
        //same entity. No entity can have the same name.
        //If you would wish to create generic UI components dont give them a
        //name or let the user provide one in the create function
        return world.Entity("LiteraturePage")
            .Add<Page>()
            .Set(new TextBlock())
            .SetText("Literature");
    }
}