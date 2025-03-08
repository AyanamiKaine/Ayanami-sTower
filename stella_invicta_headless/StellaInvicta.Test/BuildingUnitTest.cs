using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Systems;
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
        var simulationSpeed = world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.Unlocked);
        world.Import<StellaInvictaECSModule>();
        world.AddSystem(new ProductionSystem(), simulationSpeed);



        var building = world.Entity("IronMine-BUILDING")
            .Add<Building>()
            .Set<Expected, WorkForce>(new WorkForce(4000))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(5)
            ]);

        var workerPops = world.Entity()
            .Add<PopType, Worker>()
            .Add<EmployedAt>(building)
            // Here the workerpops expect to have around 200 credits in the bank
            .Set<Expected, Credits>(new Credits(200))
            // The question is what should 1 quantity of pop represent 10000k people?
            .Set<Quantity>(new(4000));

        building.Add<WorkForce>(workerPops);
        world.Progress();

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
        var simulationSpeed = world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.Unlocked);
        world.Import<StellaInvictaECSModule>();
        world.AddSystem(new ProductionSystem(), simulationSpeed);

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



        world.Progress();

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
        var simulationSpeed = world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.Unlocked);
        world.Import<StellaInvictaECSModule>();
        world.AddSystem(new ProductionSystem(), simulationSpeed);

        var building = world.Entity("IronMine-BUILDING")
            .Add<Building>()
            .Set<Level>(new(10))
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
            .Set(Literacy.FromPercentage(10))           // 5.5%
            .Set(Militancy.FromPercentage(25))         // 2.5%
            .Set(Consciousness.FromPercentage(1.5f))    // 1.5%
            .Set(Happiness.FromPercentage(40));          // 80%

        building.Add<WorkForce>(slavePops);

        world.Progress();


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
    /// Worker pops that are not slaves should be payed their wages
    /// </summary>
    [Fact]
    public void WorkerPopsGetPayed()
    {
        World world = World.Create();
        var simulationSpeed = world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.Unlocked);
        world.Import<StellaInvictaECSModule>();
        world.AddSystem(new ProductionSystem(), simulationSpeed);

        var bank = world.Entity("BANK")
            .Add<Bank>()
            .Set<DepositInterestRate>(new(0.25))
            .Set<Credits>(new(0))
            .Set<Name>(new("Bank Of Solari"));

        var building = world.Entity("IronMine-BUILDING")
            .Add<Building>()
            .Set<Wages>(new(1))
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
            .Add<Bank>(bank)
            .Add<EmployedAt>(building)
            .Set<Expected, Credits>(new Credits(0))
            .Set<Quantity>(new(2000));

        building.Add<WorkForce>(workerPops);

        // They should only get payed monthly.
        world.System<Wages>("PayingWages-System")
            .Each((Entity e, ref Wages wages) =>
            {
                var wage = wages.Value;
                e.Each<WorkForce>((e) =>
                {
                    // Wages are transfered to the pops bank,
                    // the pops will keep in mind how much money they should have
                    // but this amount might diverge from the amount the bank has
                    // because the bank may lend their money to others
                    // generating interest rates.
                    var calculatedWage = wage * e.Get<Quantity>().Value;
                    e.Target<Bank>(0).GetMut<Credits>().Ammount += calculatedWage;
                    e.GetMutSecond<Expected, Credits>().Ammount += calculatedWage;
                });
            }).Run();


        var expectedCredits = 2000;
        var actualCredits = workerPops.GetSecond<Expected, Credits>().Ammount;

        Assert.Equal(expectedCredits, actualCredits);
        Assert.Equal(expectedCredits, bank.Get<Credits>().Ammount);

    }

    /// <summary>
    /// How can we expand and scale our economy? Simply building more buildings!
    /// </summary>
    [Fact]
    public void MultipleBuildingsOnOneProvince()
    {
        World world = World.Create();
        var simulationSpeed = world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.Unlocked);
        world.Import<StellaInvictaECSModule>();
        world.AddSystem(new ProductionSystem(), simulationSpeed);

        var building1 = world.Entity("IronMine-BUILDING1")
            .Add<Building>()
            .Set<Expected, WorkForce>(new WorkForce(4000))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(10)
            ]);

        var building2 = world.Entity("IronMine-BUILDING2")
            .Add<Building>()
            .Set<Expected, WorkForce>(new WorkForce(4000))
            .Set<Inventory, GoodsList>([
            ])
            .Set<Input, GoodsList>([
            ])
            .Set<Output, GoodsList>([
                new Iron(10)
            ]);

        var workerPops1 = world.Entity()
            .Add<PopType, Worker>()
            .Add<EmployedAt>(building1)
            // Here the workerpops expect to have around 200 credits in the bank
            .Set<Expected, Credits>(new Credits(200))
            // The question is what should 1 quantity of pop represent 10000k people?
            .Set<Quantity>(new(2000))
            .Set(Literacy.FromPercentage(5.5f))           // 5.5%
            .Set(Militancy.FromPercentage(2.5f))         // 2.5%
            .Set(Consciousness.FromPercentage(1.5f))    // 1.5%
            .Set(Happiness.FromPercentage(80));          // 80%

        var workerPops2 = world.Entity()
            .Add<PopType, Worker>()
            .Add<EmployedAt>(building1)
            // Here the workerpops expect to have around 200 credits in the bank
            .Set<Expected, Credits>(new Credits(200))
            // The question is what should 1 quantity of pop represent 10000k people?
            .Set<Quantity>(new(2000))
            .Set(Literacy.FromPercentage(5.5f))           // 5.5%
            .Set(Militancy.FromPercentage(2.5f))         // 2.5%
            .Set(Consciousness.FromPercentage(1.5f))    // 1.5%
            .Set(Happiness.FromPercentage(80));          // 80%

        building1.Add<WorkForce>(workerPops1);
        building2.Add<WorkForce>(workerPops2);



        world.Progress();

        // Because the worker staff is halved we expect to only
        // produce half the amount of output iron. 
        // In this case: 10 / 2 = 5
        GoodsList expectedInventory = [
            new Iron(10)
        ];

        var actualInventory = building1.GetSecond<Inventory, GoodsList>();
        actualInventory += building2.GetSecond<Inventory, GoodsList>();
        Assert.Equal(expectedInventory, actualInventory);
    }
}
