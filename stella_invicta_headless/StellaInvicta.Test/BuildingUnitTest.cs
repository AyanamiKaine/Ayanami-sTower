using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;

public class BuildingUnitTest
{

    /// <summary>
    /// Buildings are the corner storn of the econemy they produce various goods via input.
    /// </summary>
    [Fact]
    public void BuildingProduceOutput()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var building = world.Entity("IronMine-BUILDING")
            .Add<Building>()
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(5)
            ]);


        var buildingSimulation = world.System<GoodsList, GoodsList, GoodsList>()
            .With<Building>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .TermAt(1).First<Input>().Second<GoodsList>()
            .TermAt(2).First<Output>().Second<GoodsList>()
            .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList) =>
            {
                if (inventory >= inputGoodsList)
                {
                    inventory -= inputGoodsList;
                    inventory += outputGoodsList;
                }
            });

        buildingSimulation.Run();

        GoodsList expectedInventory = [
            new Iron(5)
        ];

        var actualInventory = building.GetSecond<Inventory, GoodsList>();

        Assert.Equal(expectedInventory, actualInventory);
    }

    /// <summary>
    /// Buildings needs worker to work in, should the number 
    /// be less than expected it should decrease production.
    /// Only 50% the worker pops, then the production is only
    /// 50%
    /// </summary>
    [Fact]
    public void BuildingWorkerShortageShouldDecreaseOutput()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var building = world.Entity("IronMine-BUILDING")
            .Add<Building>()
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(10)
            ]);


        var buildingSimulation = world.System<GoodsList, GoodsList, GoodsList>()
            .With<Building>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .TermAt(1).First<Input>().Second<GoodsList>()
            .TermAt(2).First<Output>().Second<GoodsList>()
            .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList) =>
            {
                if (inventory >= inputGoodsList)
                {
                    inventory -= inputGoodsList;
                    inventory += outputGoodsList;
                }
            });

        buildingSimulation.Run();


        // Because the worker staff is halved we expect to only
        // produce half the amount of output iron. 
        // In this case: 10 / 2 = 5
        GoodsList expectedInventory = [
            new Iron(5)
        ];

        var actualInventory = building.GetSecond<Inventory, GoodsList>();

        Assert.Equal(expectedInventory, actualInventory);
    }
}
