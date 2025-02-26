using Flecs.NET.Core;

namespace StellaInvicta.Test;

public class PopulationUnitTest
{
    [Fact]
    public void Test1()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();
    }
}
