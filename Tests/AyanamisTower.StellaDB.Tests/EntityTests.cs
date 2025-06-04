using AyanamisTower.StellaDB.Model;
using SqlKata.Execution;

namespace AyanamisTower.StellaDB.Tests;

/// <summary>
/// Testing methods of the entity class
/// </summary>
public class EntityTests
{

    /// <summary>
    /// We should be able to easily add identifier components
    /// </summary>
    [Fact]
    public void AddingIdentifierComponents()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var characterEntity = world.Entity("Example Character");
        characterEntity.Add<Character>();
        /* Same as
        world.Query("Character").Insert(new
        {
            EntityId = characterEntity.Id
        });
        */

        Assert.True(characterEntity.Has<Character>());
    }

    /// <summary>
    /// We should be able to add components with values for an entity
    /// </summary>
    [Fact]
    public void AddingComponentWithValues()
    {
        var world = new World("TEST", true);

        var characterEntity = world.Entity("Example Character");
        characterEntity.Add<Character>();

        const string newName = "Changed Name";
        characterEntity.Update(new Name() { Value = newName });

        Assert.Equal(newName, characterEntity.Get<Name>().Value);
    }
}
