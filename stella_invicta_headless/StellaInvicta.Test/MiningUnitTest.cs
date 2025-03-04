using Flecs.NET.Core;

namespace StellaInvicta.Test;


public class MiningUnitTest
{

    /// <summary>
    /// Here we want to create a system of mining, so instead of manaully going over entities
    /// we add a relation like Mining(Astroid), where mining is a tag and astroid an entity.
    /// A system would work on entities that have the mining relationship with another entity 
    /// and would start mining that. 
    /// Simply removing the tag stops the mining, adding it starts it.
    /// </summary>
    [Fact]
    public void MiningUsingTagsRelationshipsAndSystems()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        Assert.True(false);
    }
}
