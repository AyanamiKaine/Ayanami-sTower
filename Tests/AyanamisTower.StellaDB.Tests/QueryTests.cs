using SqlKata.Execution;

namespace AyanamisTower.StellaDB.Tests;

/// <summary>
/// We want to test various queries that we expect to work out of the box
/// </summary>
public class QueryTests
{
    /// <summary>
    /// We have the ability to dynamically add and remove features from entities.
    /// the prime example is for a character entity to have a feature called can_marry
    /// </summary>
    [Fact]
    public void QueringFeatureFlagsToEntities()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var characterEntity = world.Entity("Example Character");

        //Identifies the entity as a character
        world.Query("Character").Insert(new
        {
            EntityId = characterEntity.Id
        });

        world.Query("EntityFeature").Insert(new
        {
            EntityId = characterEntity.Id,
            FeatureId = world.Query("FeatureDefinition")
                .Where("Key", "can_marry") // the can_marry feature definition is a pre defined one
                .Select("EntityId")
                .FirstOrDefault<long>()
        });

        var canCharacterMarry = world.Query("EntityFeature as ef")
            .Join("FeatureDefinition as fd", "ef.FeatureId", "fd.EntityId")
            .Where("ef.EntityId", characterEntity.Id)
            .Where("fd.Key", "can_marry")
            .Count<int>() > 0;

        Assert.True(canCharacterMarry);
    }
}
