using System.Numerics;
using Flecs.NET.Core;
using NLog.LayoutRenderers;
using StellaInvicta.Components;
using StellaInvicta.Tags.CelestialBodies;
using StellaInvicta.Tags.Identifiers;
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
            .Set<Position, Vector3>(Vector3.Zero)
            .Add<LocatedAt>(galaxy);

        var planet = world.Entity("Tomaxa")
            .Add<Planet>()
            .Set<Quaternion>(new())
            .Set<Mass>(new(1.0f))
            .Set<Position, Vector3>(new Vector3(x: 20, y: 50, z: 70))
            .Set<Velocity, Vector3>(new Vector3(x: 0, y: 0, z: 10))
            .Add<Orbits>(starSystem);

        Assert.True(planet.Orbits(starSystem));
        Assert.False(planet.Orbits(galaxy));

        world.Progress();

        // The planet should not have its inital position after rotating a bit around its star
        Assert.NotEqual(new Vector3(x: 20, y: 50, z: 70), planet.GetSecond<Position, Vector3>());
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
