using Flecs.NET.Core;

namespace StellaInvicta.Test;

public class FactoryUnitTest
{
    [Fact]
    public void Test1()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();
    }
}
