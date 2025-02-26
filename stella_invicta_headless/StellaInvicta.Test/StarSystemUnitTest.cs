using Flecs.NET.Core;
using NLog.LayoutRenderers;
using StellaInvicta.Components;
using StellaInvicta.Tags.CelestialBodies;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Test;

/// <summary>
/// Here we want to test the simulation for one star system.
/// </summary>
public class StarSystemUnitTest
{
    /// <summary>
    /// Objects orbiting other objects should get their positions updated, 
    /// because they are moving around an object.
    /// </summary>
    [Fact]
    public void PlanetOrbitingStar()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var galaxy = world.Entity("Andromeda")
            .Add<Galaxy>();

        var starSystem = world.Entity("Mexo-5")
            .Add<Star>()
            .Set<Position3D>(new(X: 0, Y: 0, Z: 0))
            .Add<LocatedAt>(galaxy);

        var planet = world.Entity("Tomaxa")
            .Add<Planet>()
            .Set<Position3D>(new(X: 20, Y: 50, Z: 70))
            .Add<Orbits>(starSystem);

        Assert.True(planet.Orbits(starSystem));
        Assert.False(planet.Orbits(galaxy));

        world.Progress();

        // The planet should not have its inital position after rotating a bit around its star
        Assert.NotEqual(new Position3D(X: 20, Y: 50, Z: 70), planet.Get<Position3D>());
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
