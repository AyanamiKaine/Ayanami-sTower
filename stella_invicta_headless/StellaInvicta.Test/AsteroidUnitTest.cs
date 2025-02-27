using System.Numerics;
using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.CelestialBodies;
using StellaInvicta.Tags.Identifiers;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Test;

public class AsteroidUnitTest
{
    [Fact]
    public void AsteroidsHaveResources()
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



        var miningShip = world.Entity()
            .Add<LocatedAt>(starSystem)
            .Set<Position, Vector3>(new Vector3(x: 50, y: 0, z: 0))
            .Add<Miner>();

        var asteroid = world.Entity()
            .Add<Asteroid>()
            .Add<Orbits>(planet)
            .Set<Position, Vector3>(new Vector3(x: 50, y: 0, z: 0))
            .Set<Gold>(new(200));

        // After the shiped mined some resources, 
        // the astroid should have less gold than initally

        miningShip.Mine<Gold>(asteroid);
        Assert.True(asteroid.Get<Gold>().Quantity < 200);
    }

    [Fact]
    public void CantMineBelowZero()
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


        var miningShip = world.Entity()
            .Add<LocatedAt>(starSystem)
            .Set<Position, Vector3>(new Vector3(x: 50, y: 0, z: 0))
            .Add<Miner>();

        var asteroid = world.Entity()
            .Add<Asteroid>()
            .Add<Orbits>(planet)
            .Set<Position, Vector3>(new Vector3(x: 50, y: 0, z: 0))
            .Set<Gold>(new(10));

        // After the shiped mined some resources, 
        // the astroid should have less gold than initally

        miningShip.Mine<Gold>(asteroid);
        Assert.Equal(0, asteroid.Get<Gold>().Quantity);
    }
}
