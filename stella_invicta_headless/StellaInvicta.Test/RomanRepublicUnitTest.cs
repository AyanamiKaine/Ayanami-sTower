using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;



/*
Production Chains
Unlike modern economies, ancient production was simpler but still had multiple stages:

Basic Chains:

Grain → Flour → Bread
Grapes → Wine
Olives → Olive Oil
Flax → Linen
Clay → Pottery


Advanced Chains:

Copper + Tin → Bronze → Weapons/Tools
Iron Ore → Iron → Advanced Weapons/Tools
Wood → Lumber → Ships/Buildings
Wool → Textiles → Clothing
*/

/// <summary>
/// Can we use our economy system, 
/// to model a small roman republic?
/// </summary>
public class RomanRepublicUnitTest
{
    /// <summary>
    /// In ancients times the biggest contriburter to the economy was
    /// extracting raw resources, a small manufacturing and of course by large
    ///  80-90% was just agriculture.
    /// </summary>
    [Fact]
    public void RomanEconomy()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var Latium = world.Entity("Latium-PROVINCE")
            .Add<Province>()
            .Set<RawResource, GoodsList>([]);

        var Etruria = world.Entity("Etruria-PROVINCE")
            .Add<Province>()
            .Set<RawResource, GoodsList>([]);


        var grainFarm = world.Entity("grainFarm-BUILDING")
             .Add<Building>()
             .Set<Expected, WorkForce>(new WorkForce(100))
             .Set<Inventory, GoodsList>([
             ])
             .Set<Input, GoodsList>([
             ])
             .Set<Output, GoodsList>([
                 new Grain(100)
             ]);

        var mill = world.Entity("mill-BUILDING")
            .Add<Building>()
            .Set<Expected, WorkForce>(new WorkForce(20))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Wood(10)
            ]);

        var lignatores = world.Entity("lignatores-BUILDING")
            .Add<Building>()
            .Set<Expected, WorkForce>(new WorkForce(20))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Wood(10)
            ]);

        Assert.Fail();
    }
}
