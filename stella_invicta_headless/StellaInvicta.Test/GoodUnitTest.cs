using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;

/// <summary>
/// Goods are the underlying material in the econemy,
/// they should not be hard coded, but also efficient to work
/// with the current ECS system.
/// 
/// A goods class will be accessed many many times, they should be 
/// really good optimized.
/// </summary>
public class GoodUnitTest
{

    /// We need a working nice way of defining goods, the question
    /// is should we define goods as entities or as objects?

    /// <summary>
    /// Here we try to define goods as objects
    /// </summary>
    [Fact]
    public void DefineNewGoodObject()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        GoodsList inventory =
        [
            new Iron(15)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        Assert.True(inventory >= input);
    }
}
