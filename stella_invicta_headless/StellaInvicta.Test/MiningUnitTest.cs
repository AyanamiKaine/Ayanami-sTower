using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;


public class MiningUnitTest
{

    struct Mining { };

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

        var asteroid = world.Entity()
            .Set<GoodsList>([]);

        var miningShip = world.Entity()
            .Add<Mining>(asteroid)
            .Set<Inventory, GoodsList>([
                new Iron(20)
            ]);

        world.System<Entity, GoodsList>()
            .TermAt(0).First<Mining>().Second<Entity>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .Each((Entity miningEntity, ref Entity EntityToBeMined, ref GoodsList miningEntityInventory) =>
            {
                miningEntityInventory += EntityToBeMined.Get<GoodsList>();
            });

        world.Progress();

        var expectedInventory = new GoodsList()
        {
            new Iron(20)
        };

        Assert.Equal(expectedInventory, miningShip.GetSecond<Inventory, GoodsList>());
        Assert.Fail("Sadly this is not done yet, because we need to think about: How much should be mined, etc.");
    }
}
