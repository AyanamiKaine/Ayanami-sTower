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


        var building = world.Entity("ClothingFactory-BUILDING")
            .Add<Building>()
            .Set<Inventory, GoodsList>([
                new Iron(1)
            ])
            .Set<Input, GoodsList>([
                new Iron(1)
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

        world.Progress();

        GoodsList expectedInventory = [
            new Iron(5)
        ];

        var actualInventory = building.GetSecond<Inventory, GoodsList>();

        Assert.Equal(expectedInventory, actualInventory);
    }
}
