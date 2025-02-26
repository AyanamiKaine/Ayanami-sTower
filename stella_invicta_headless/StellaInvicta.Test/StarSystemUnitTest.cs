using Flecs.NET.Core;

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
    }
}
