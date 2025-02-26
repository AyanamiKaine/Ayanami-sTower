using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Systems;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Test;

public class GameTimeUnitTest
{
    [Fact]
    public void GameTimeProgessDays()
    {
        World world = World.Create();

        world.Import<StellaInvictaECSModule>();

        world.Set<GameDate>(new(year: 0, month: 1, day: 1));

        world.AddSystem(new GameTimeSystem());

        var expectedGameDate = new GameDate(year: 0, month: 1, day: 30);

        for (int i = 0; i < 29; i++)
        {
            world.Progress();
        }

        Assert.Equal(expectedGameDate, world.Get<GameDate>());
    }

    /// <summary>
    /// Progess 30 days so one month elapsed
    /// </summary>
    [Fact]
    public void GameTimeProgessMonth()
    {
        World world = World.Create();

        world.Import<StellaInvictaECSModule>();

        world.Set<GameDate>(new(year: 0, month: 1, day: 1));

        world.AddSystem(new GameTimeSystem());

        var expectedGameDate = new GameDate(year: 0, month: 2, day: 1);

        for (int i = 0; i < 30; i++)
        {
            world.Progress();
        }

        Assert.Equal(expectedGameDate, world.Get<GameDate>());
    }

    /// <summary>
    /// Progess 360 days so one year elapsed
    /// </summary>
    [Fact]
    public void GameTimeProgessYear()
    {
        World world = World.Create();

        world.Import<StellaInvictaECSModule>();

        world.Set<GameDate>(new(year: 0, month: 1, day: 1));

        world.AddSystem(new GameTimeSystem());

        var expectedGameDate = new GameDate(year: 1, month: 1, day: 1);

        for (int i = 0; i < 360; i++)
        {
            world.Progress();
        }

        Assert.Equal(expectedGameDate, world.Get<GameDate>());
    }
}
