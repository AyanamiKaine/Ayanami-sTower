using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.Identifiers;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Test;

public class CharacterUnitTest
{
    [Fact]
    public void CharactersShouldAge()
    {
        World world = World.Create();
        var simulationSpeed = world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.Unlocked);
        world.Import<StellaInvictaECSModule>();


        // Set Current Year
        world.Set<GameDate>(new(year: 29, month: 12, day: 1));

        world.AddSystem(new GameTimeSystem(), simulationSpeed);
        world.AddSystem(new AgeSystem(), simulationSpeed);

        var Marina = world.Entity("Marina")
            .Set<Birthday, GameDate>(new GameDate(year: 0, month: 1, day: 1))
            .Add<Character>()
            .Set<Name>(new("Marina"))
            .Set<Age>(new(29));

        var expectedAge = 30;

        for (int i = 0; i < 60; i++)
        {
            world.Progress();
        }

        Assert.Equal(expectedAge, Marina.Get<Age>().Value);
    }

    /// <summary>
    /// Characters should seek out marriages and birth new childs
    /// (DESIGN NOTE: maybe we could add a way for species to mix and match
    /// similar to how its done in Rimworld, using genes, extracting and injecting them)
    /// </summary>
    [Fact]
    public void CharactersShouldSeekOutMarriages()
    {
        Assert.Fail();
    }
}
