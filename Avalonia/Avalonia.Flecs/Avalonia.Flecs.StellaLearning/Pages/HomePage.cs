using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// Defines an interface for creating UI components as entities
/// </summary>
interface IEntityUIComponent
{


    /* ABOUT ENTITY NAMES
    If you give an entity a name it can be later used to identify it
    It becomes a unique entity, two entities with the same name are the
    same entity. No entity can have the same name.
    If you would wish to create generic UI components dont give them a
    name or let the user provicde one in the create function.

    Just remember that entity names are based on paths. This means to query it
    it sensetive to its parents path.
    */
    /// <summary>
    /// Creates a UI component entity that may be given a inital name
    /// </summary>
    /// <param name="world">Flecs ECS World where the entity gets created in</param>
    /// <param name="entityName">Name of the created UI Entity. If none is the id of the entity is used instead</param>
    /// <returns></returns>
    static abstract Entity Create(World world, string entityName = "");
    /// <summary>
    /// Creates a UI component entity with the given props
    /// </summary>
    /// <param name="world">Flecs ECS World where the entity gets created in</param>
    /// <param name="props">Used to pass down arguments, similar to props in react</param>
    /// <returns></returns>
    static abstract Entity Create(World world, params object[] props);
    static abstract Entity Create(World world, string entityName = "", params object[] props);
    static abstract Entity Create(World world, Entity childOfEntity);
    static abstract Entity Create(World world, Entity childOfEntity, string entityName = "");
    static abstract Entity Create(World world, Entity childOfEntity, params object[] props);
    static abstract Entity Create(World world, Entity childOfEntity, string entityName = "", params object[] props);

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
        return world.Entity()
            .Add<Page>()
            .ChildOf(childOfEntity)
            .Set(new TextBlock()
            {
                Text = "Home",
                Margin = new Thickness(10)
            });
    }

    public static Entity Create(World world, Entity childOfEntity, params object[] props)
    {
        throw new System.NotImplementedException();
    }

    public static Entity Create(World world, string entityName = "", params object[] props)
    {
        throw new System.NotImplementedException();
    }

    public static Entity Create(World world, Entity childOfEntity, string entityName = "", params object[] props)
    {
        throw new System.NotImplementedException();
    }

    public static Entity Create(World world, Entity childOfEntity, string entityName = "")
    {
        return world.Entity(entityName)
            .Add<Page>()
            .ChildOf(childOfEntity)
            .Set(new TextBlock()
            {
                Text = "Home",
                Margin = new Thickness(10)
            });
    }
}
