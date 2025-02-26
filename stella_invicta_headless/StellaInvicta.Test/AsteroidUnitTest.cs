using Flecs.NET.Core;
using StellaInvicta.Tags.CelestialBodies;

namespace StellaInvicta.Test;

public class AsteroidUnitTest
{
    [Fact]
    public void AsteroidsHaveResources()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var asteroid = world.Entity()
            .Add<Asteroid>();

    }
}
