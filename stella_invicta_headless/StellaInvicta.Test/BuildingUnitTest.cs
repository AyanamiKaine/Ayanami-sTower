using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;
using StellaInvicta.Tags.Relationships;

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
            .Set<Expected, WorkForce>(new WorkForce(4000))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(10)
            ]);

        var workerPops = world.Entity()
            .Add<PopType, Worker>()
            .Add<EmployedAt>(building)
            // Here the workerpops expect to have around 200 credits in the bank
            .Set<Expected, Credits>(new Credits(200))
            // The question is what should 1 quantity of pop represent 10000k people?
            .Set<Quantity>(new(2000))
            .Set(Literacy.FromPercentage(5.5f))           // 5.5%
            .Set(Militancy.FromPercentage(2.5f))         // 2.5%
            .Set(Consciousness.FromPercentage(1.5f))    // 1.5%
            .Set(Happiness.FromPercentage(80));          // 80%

        building.Add<WorkForce>(workerPops);

        var buildingSimulation = world.System<GoodsList, GoodsList, GoodsList, WorkForce>()
            .With<Building>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .TermAt(1).First<Input>().Second<GoodsList>()
            .TermAt(2).First<Output>().Second<GoodsList>()
            .TermAt(3).First<Expected>().Second<WorkForce>()
            .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList, ref WorkForce expectedWorkForce) =>
            {
                var employedWorkForce = 0;
                e.Each<WorkForce>(e =>
                {
                    employedWorkForce += e.Get<Quantity>().Value;
                });

                var employmentRatio = Math.Clamp((double)employedWorkForce / expectedWorkForce.Value, 0, 1);

                if (inventory >= inputGoodsList)
                {
                    inventory -= inputGoodsList;
                    inventory += outputGoodsList *= employmentRatio;
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

    /// <summary>
    /// The slave tag has the IsA relationship tag with the worker tag.
    /// So we can use the slave tag in places where the worker tag is 
    /// expected.
    /// </summary>
    [Fact]
    public void BuildingWorkerSlavesCouldBeUsedInPlaceOfWorkers()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var building = world.Entity("IronMine-BUILDING")
            .Add<Building>()
            .Set<Expected, WorkForce>(new WorkForce(4000))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(10)
            ]);

        var slavePops = world.Entity()
            .Add<PopType, Slave>()
            .Add<EmployedAt>(building)
            // Here the workerpops expect to have around 200 credits in the bank
            .Set<Expected, Credits>(new Credits(200))
            // The question is what should 1 quantity of pop represent 10000k people?
            .Set<Quantity>(new(2000))
            .Set(Literacy.FromPercentage(5.5f))           // 5.5%
            .Set(Militancy.FromPercentage(2.5f))         // 2.5%
            .Set(Consciousness.FromPercentage(1.5f))    // 1.5%
            .Set(Happiness.FromPercentage(80));          // 80%

        building.Add<WorkForce>(slavePops);

        var buildingSimulation = world.System<GoodsList, GoodsList, GoodsList, WorkForce>()
            .With<Building>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .TermAt(1).First<Input>().Second<GoodsList>()
            .TermAt(2).First<Output>().Second<GoodsList>()
            .TermAt(3).First<Expected>().Second<WorkForce>()
            .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList, ref WorkForce expectedWorkForce) =>
            {
                var employedWorkForce = 0;
                e.Each<WorkForce>(e =>
                {
                    employedWorkForce += e.Get<Quantity>().Value;
                });

                var employmentRatio = Math.Clamp((double)employedWorkForce / expectedWorkForce.Value, 0, 1);

                if (inventory >= inputGoodsList)
                {
                    inventory -= inputGoodsList;
                    inventory += outputGoodsList *= employmentRatio;
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
