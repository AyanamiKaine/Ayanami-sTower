using Flecs.NET.Core;
using NLog.LayoutRenderers;
using StellaInvicta.Tags.CelestialBodies;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Test;

/// <summary>
/// Here we want to test the simulation for one star system.
/// </summary>
public class StarSystemUnitTest
{
    [Fact]
    public void Test1()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var galaxy = world.Entity("Andromeda")
            .Add<Galaxy>();

        var starSystem = world.Entity("Mexo-5")
            .Add<Star>()
            .Add<LocatedAt>(galaxy);

        var planet = world.Entity("Tomaxa")
            .Add<Planet>()
            .Add<Orbits>(starSystem);

    }

    [Fact]
    public void StarSystemsConnections()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var solarSystem = world.Entity("solarSystem")
            .Add<Star>();

        var alphaCentauri = world.Entity("alphaCentauri")
            .Add<Star>()
            .Add<ConnectedTo>(solarSystem);

        // This star system is not connected to the solar system ONLY to alpha centauri
        var NotConnectedStarSystem = world.Entity()
            .Add<Star>()
            .Add<ConnectedTo>(alphaCentauri);

        Assert.True(solarSystem.ConnectedTo(alphaCentauri));
        Assert.False(NotConnectedStarSystem.ConnectedTo(solarSystem));
    }
}
