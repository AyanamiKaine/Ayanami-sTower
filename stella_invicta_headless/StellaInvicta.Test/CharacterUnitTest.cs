using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Test;

public class CharacterUnitTest
{
    [Fact]
    public void CharactersShouldAge()
    {
        World world = World.Create();

        world.Import<StellaInvictaECSModule>();


        // Current Year
        world.Set<GameDate>(new(year: 29, month: 12, day: 1));

        world.AddSystem(new GameTimeSystem());
        world.AddSystem(new AgeSystem());

        var Marina = world.Entity("Marina")
            .Set<Birthday, GameDate>(new GameDate(year: 0, month: 1, day: 1))
            .Set<Name>(new("Marina"))
            .Set<Age>(new(29));

        var expectedAge = 30;

        for (int i = 0; i < 60; i++)
        {
            world.Progress();
        }

        Assert.Equal(expectedAge, Marina.Get<Age>().Value);
    }
}
