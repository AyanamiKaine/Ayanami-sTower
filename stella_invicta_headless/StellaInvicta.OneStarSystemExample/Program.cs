using Flecs.NET.Core;
using StellaInvicta;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.Identifiers;
using StellaInvicta.Tags.Relationships;

var world = World.Create();
world.Import<StellaInvictaECSModule>();
world.Import<Ecs.Stats>();


var simulationSpeed = world.Timer("SimulationSpeed")
    .Interval(SimulationSpeed.Default);

// Set Current Year
world.Set<GameDate>(new(year: 29, month: 12, day: 1));
world.AddSystem(new GameTimeSystem(), simulationSpeed);
world.AddSystem(new AgeSystem(), simulationSpeed);


var random = new Random();
for (int i = 0; i < 28677; i++)
{
    int randomYear = random.Next(0, 29); // Random year between 0 and 28
    int randomMonth = random.Next(1, 13); // Random month between 1 and 12
    int randomDay = random.Next(1, 29); // Random day between 1 and 28 (simplified)

    world.Entity()
        .Set<Birthday, GameDate>(new GameDate(year: randomYear, month: randomMonth, day: randomDay))
        .Add<Character>()
        .Set<Name>(new($"Marina_{i}"))
        .Set<Age>(new(29 - randomYear)); // Adjust age based on birth year
}

world.App().EnableRest().EnableStats().Run();
