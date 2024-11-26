using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Pages;

interface IEntityUIComponent
{
    static abstract Entity Create(World world, string entityName = "");
    static abstract Entity Create(World world, params object[] props);

    static abstract Entity Create(World world, Entity childOfEntity);
    static abstract Entity Create(World world, Entity childOfEntity, params object[] props);

}

public class HomePage : IEntityUIComponent
{
    public static Entity Create(World world, string entityName = "")
    {
        //If you give an entity a name it can be later used to identify it
        //It becomes a unique entity, two entities with the same name are the
        //same entity. No entity can have the same name.
        //If you would wish to create generic UI components dont give them a
        //name or let the user provicde one in the create function
        return world.Entity(entityName)
            .Add<Page>()
            .Set(new TextBlock()
            {
                Text = "Home",
                Margin = new Thickness(10)
            });
    }
    public static Entity Create(World world, params object[] props)
    {
        throw new System.NotImplementedException();
    }
    public static Entity Create(World world, Entity childOfEntity)
    {
        throw new System.NotImplementedException();
    }

    public static Entity Create(World world, Entity childOfEntity, params object[] props)
    {
        throw new System.NotImplementedException();
    }
}
