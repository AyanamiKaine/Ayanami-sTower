using Flecs.NET.Core;
using StellaInvicta;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.CelestialBodies;
using StellaInvicta.Tags.Identifiers;
using StellaInvicta.Tags.Relationships;

var world = World.Create();
world.Import<StellaInvictaECSModule>();
world.Import<Ecs.Stats>();


var simulationSpeed = world.Timer("SimulationSpeed")
    .Interval(SimulationSpeed.Unlocked);

var gameDate = new GameDate(year: 29, month: 12, day: 1);


// Set Current Year
world.Set<GameDate>(new(year: 29, month: 12, day: 1));
world.AddSystem(new GameTimeSystem(), simulationSpeed);
world.AddSystem(new AgeSystem(), simulationSpeed);

var solarSystem = world.Entity("solarSystem-STARSYSTEM")
    .Add<Star>();

var alphaCentauri = world.Entity("alphaCentauri-STARSYSTEM")
    .Add<Star>()
    .Add<ConnectedTo>(solarSystem);

world.Entity("Random StarSysten")
    .Add<Star>()
    .Add<ConnectedTo>(alphaCentauri);

var solarum = world.Entity("Solarum-PLANET")
    .Add<Planet>();

var aurelianReach = world.Entity("aurelianReach-PROVINCE")
    .Add<Planet>(solarum)
    .Add<Province>();

var solariGoldLifeNeed = world.Entity("SolariGold-LIFENEED")
            .Add<LifeNeed>()
            .Set<Name>(new("Gold"))
            .Set<ShortDescription>(new("Solari consume gold as part of their diet."))
            .Set<Quantity>(new(20));

var solariFishLifeNeed = world.Entity("SolariFish-LIFENEED")
    .Add<LifeNeed>()
    .Set<Name>(new("Fish"))
    .Set<ShortDescription>(new("Solari also quite like fish"))
    .Set<Quantity>(new(25));

// We define a species as an entity.
// With that we can give the species various different traits.
var solari = world.Entity("Solari-SPECIE")
    .Add<Specie>()
    .Add<LifeNeed>(solariGoldLifeNeed)
    .Add<LifeNeed>(solariFishLifeNeed)
    .Set<ShortDescription>(new("The solari are a specie that bathed in the sun."))
    .Set<Name>(new("Solari"));

// Something similar goes for cultures
// For example we can define that specifc cultures 
// demand specific goods like a mineral used for culture 
// purposes
var solmantum = world.Entity("Solmantum-CULTURE")
    .Add<Culture>()
    .Set<Name>(new("solmantum"));

var sharedProsperityDoctrine = world.Entity("Shared-Prosperity-Doctrine-IDEOLOGY")
    .Add<Ideology>()
    .Set<Name>(new("Shared Prosperity Doctrine"))
    .Set<ShortDescription>(new("Advocates for social equality and the empowerment of the working class."))
    .Set<LongDescription>(new(
        """
                Shared Prosperity Doctrine, as a political ideology, promises a radical restructuring of society with a focus on social equality and worker empowerment.  In contrast to the perceived inequalities of the Golden Rule Charter, Shared Prosperity Doctrine champions the rights of the working class and advocates for a more equitable distribution of wealth and resources.

                Pops adhering to Doctrine ideals will likely demand significant social reforms, such as improved working conditions, minimum wages, and social welfare programs. They may also agitate for political reforms that grant greater power to the working class and potentially challenge existing hierarchies and power structures.

                Economically, Doctrine pops may favor state intervention and control over the economy, potentially leading to demands for nationalization of industries and a shift away from the Golden Rule Charter.  High militancy among Shared Prosperity pops can lead to unrest and even revolution if their demands are not met, but successfully managing them can unlock powerful social and economic reforms that reshape your nation.
                """));


// Pops dont hold their own money they store it in a bank.
// The bank stores the money and lends it out.
// A bank has a global money vault, as it lends out money and pops
// take money from the bank they believe they have everything works
// if the bank lost too much money and cannot give pops any money anymore
// all hell breaks lose.
var federalSolariBank = world.Entity("Bank-Of-Solari-BANK")
    .Add<Bank>()
    .Set<Name>(new("Bank Of Solari"));

/*
Also imagine being highly indebt in the golden exchange and then destroying it!
all your debt GONE!
*/

var eternalCycleLuxuryFishNeed = world.Entity()
    .Add<LuxuryNeed>()
    .Set<Name>(new("Fish"))
    .Set<Quantity>(new(5));

var eternalCycle = world.Entity("Eternal-Cycle-RELIGION")
    .Add<Religion>()
    .Add<LuxuryNeed>(eternalCycleLuxuryFishNeed)
    .Set<Name>(new("Eternal Cycle"))
    .Set<ShortDescription>(new("Time is cyclical. The universe is born, dies, and is reborn in an endless cycle."));

var kryllBloodFaith = world.Entity("Kryll Blood-RELIGION")
    .Add<Religion>()
    .Set<Name>(new("Kryll Blood-Faith"))
    .Set<ShortDescription>(new("The Kryll are the chosen species, destined to rule the galaxy. Strength, conquest, and the shedding of blood are sacred acts. Their ancestors watch from the afterlife, judging their worthiness."));

var workerIronLifeNeed = world.Entity()
    .Add<LifeNeed>()
    .Set<Name>(new("Iron"))
    .Set<Quantity>(new(10));

var worker = world.Entity("Worker-POPTYPE")
    .Add<PopType>()
    .Add<LifeNeed>(workerIronLifeNeed)
    .Set<Name>(new("Worker"))
    .Set<ShortDescription>(new("Workers for the basic work"));


var workerPops = world.Entity("workerPopsOfAurelianReach-POPULATION")
    .Add<Province>(aurelianReach)
    .Add<PopType>(worker)
    .Add<Bank>(federalSolariBank)
    .Add<Culture>(solmantum)
    .Add<Specie>(solari)
    .Add<Ideology>(sharedProsperityDoctrine)
    .Add<Religion>(eternalCycle)
    .Add<LocatedAt>()
    .Set<Literacy>(new(0.0f))
    .Set<Militancy>(new(0.0f))
    .Set<Consciousness>(new(0.0f))
    .Set<Happiness>(new(0.0f));

Random rand = new();

// Here we are creating 100.000 buildings that will be simulated
for (int i = 0; i < 100000; i++)
{
    var building = world.Entity()
    .Add<Building>()
    .Set<Expected, WorkForce>(new WorkForce(4000))
    .Set<Actual, WorkForce>(new WorkForce(2000))
    .Set<Inventory, GoodsList>([
        new Iron(rand.Next(0,2000))
    ])
    .Set<Input, GoodsList>([
        new Iron(rand.Next(0,20))
    ])
    .Set<Output, GoodsList>([
        new Iron(rand.Next(0,7))
    ]);

    var workerPops2 = world.Entity()
        .Add<PopType, Worker>()
        .Add<EmployedAt>(building)
        // Here is workerpops expect to have around 200 credits in the bank
        .Set<Expected, Credits>(new Credits(200))
        // The question is what should 1 quantity of pop represent 10000k people?
        .Set<Quantity>(new(2000))
        .Set(Literacy.FromPercentage(5.5f))           // 5.5%
        .Set(Militancy.FromPercentage(2.5f))         // 2.5%
        .Set(Consciousness.FromPercentage(1.5f)) // 1.5%
        .Set(Happiness.FromPercentage(80));          // 80%

    building.Add<WorkForce>(workerPops);

}

var buildingSimulation = world.System<GoodsList, GoodsList, GoodsList>("Building Simulation")
    // When defined as multithreaded its execution will be split based on the set threads
    // so lets say we have 100.000 entities this system matches and 32 threads.
    // then one thread will iterate over (100.000 / 32) entities. 
    .MultiThreaded()
    .With<Building>()
    .TermAt(0).First<Inventory>().Second<GoodsList>()
    .TermAt(1).First<Input>().Second<GoodsList>()
    .TermAt(2).First<Output>().Second<GoodsList>()
    .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList) =>
    {
        if (inventory >= inputGoodsList)
        {
            inventory -= inputGoodsList;
            inventory += outputGoodsList *= 0.5;
        }
    });

var marina = EntityCreator.CreateCharacter(
    world: world,
    locatedAt: aurelianReach,
    culture: solmantum,
    specie: solari,
    religion: eternalCycle,
    birthDate: new GameDate(year: 0, month: 1, day: 1),
    characterName: "Marina",
    age: 29);

world.SetTargetFps(120);
world.App().EnableRest().EnableStats().Run();
