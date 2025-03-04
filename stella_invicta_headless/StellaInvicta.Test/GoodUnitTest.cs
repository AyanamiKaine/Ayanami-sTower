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
    class ModGood(int quantity) : Good("ModdedGood", quantity)
    {
        public override IGood WithQuantity(int newQuantity)
        {
            return new ModGood(newQuantity);
        }
    }

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
            // We can easily define new goods by 
            // simply extending the base type.
            new ModGood(5),
            new Iron(15)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        Assert.True(inventory >= input);
    }

    [Fact]
    public void EqualOperatorGoods()
    {
        Iron iron1 = new(10);
        Iron iron2 = new(10);

        Assert.True(iron1 == iron2);
    }

    [Fact]
    public void SubtractOperatorGoods()
    {
        Iron iron1 = new(10);
        Iron iron2 = new(10);

        var expectedIron = new Iron(0);
        Assert.Equal(expectedIron, iron1 - iron2);
    }

    [Fact]
    public void AddOperatorGoods()
    {
        Iron iron1 = new(10);
        Iron iron2 = new(10);

        var expectedIron = new Iron(20);
        Assert.Equal(expectedIron, iron1 + iron2);
    }

    [Fact]
    public void GreaterOperatorGoods()
    {
        Iron iron1 = new(20);
        Iron iron2 = new(10);

        Assert.True(iron1 > iron2);
    }

    [Fact]
    public void GreaterOrEqualOperatorGoodsUnitTest1()
    {
        Iron iron1 = new(10);
        Iron iron2 = new(10);

        Assert.True(iron1 >= iron2);
    }


    [Fact]
    public void GreaterOrEqualOperatorGoodsUnitTest2()
    {
        Iron iron1 = new(20);
        Iron iron2 = new(10);

        Assert.True(iron1 >= iron2);
    }

    [Fact]
    public void LessOperatorGoodsUnitTest()
    {
        Iron iron1 = new(5);
        Iron iron2 = new(10);

        Assert.True(iron1 < iron2);
    }

    [Fact]
    public void LessOrEqualOperatorGoodsUnitTest1()
    {
        Iron iron1 = new(10);
        Iron iron2 = new(10);

        Assert.True(iron1 <= iron2);
    }

    [Fact]
    public void LessOrEqualOperatorGoodsUnitTest2()
    {
        Iron iron1 = new(5);
        Iron iron2 = new(10);

        Assert.True(iron1 <= iron2);
    }
}
