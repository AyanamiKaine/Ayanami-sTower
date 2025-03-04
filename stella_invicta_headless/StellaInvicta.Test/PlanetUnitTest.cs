using Flecs.NET.Core;

namespace StellaInvicta.Test;

/// <summary>
/// Here we want to test the simulation for one planet.
/// </summary>
public class PlanetUnitTest
{
    [Fact]
    public void DefineAPlanet()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        Assert.Fail();
    }
}
