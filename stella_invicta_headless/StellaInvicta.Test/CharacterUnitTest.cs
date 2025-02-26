using Flecs.NET.Core;
using StellaInvicta.Components;

namespace StellaInvicta.Test;

public class CharacterUnitTest
{
    [Fact]
    public void CharactersShouldAge()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var Marina = world.Entity("Marina")
            .Set<Name>(new("Marina"))
            .Set<Age>(new(29));


    }
}
