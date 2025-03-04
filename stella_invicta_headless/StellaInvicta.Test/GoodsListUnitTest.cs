using Flecs.NET.Core;
using StellaInvicta.Components;

namespace StellaInvicta.Test;

/// <summary>
/// For the goods list we define various operators in combination with 
/// the goods type, it would not good if anything there breaks.
/// </summary>
public class GoodsListUnitTest
{
    [Fact]
    public void GreaterThen()
    {
        GoodsList inventory =
        [
            new Iron(15)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        Assert.True(inventory > input);
    }

    [Fact]
    public void GreaterOrEqualThen()
    {
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

    [Fact]
    public void LessThen()
    {
        GoodsList inventory =
        [
            new Iron(10)
        ];

        GoodsList input =
        [
            new Iron(15)
        ];

        Assert.True(inventory < input);
    }

    [Fact]
    public void LessOrEqualThen()
    {
        GoodsList inventory =
        [
            new Iron(15)
        ];

        GoodsList input =
        [
            new Iron(15)
        ];

        Assert.True(inventory <= input);
    }

    [Fact]
    public void Equal()
    {
        GoodsList inventory =
        [
            new Iron(10)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        Assert.True(inventory == input);
    }

    [Fact]
    public void NotEqual()
    {
        GoodsList inventory =
        [
            new Iron(5)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        Assert.True(inventory != input);
    }

    [Fact]
    public void SubtractGoodsList()
    {
        GoodsList inventory =
        [
            new Iron(15)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        GoodsList expectedInventory = [
            new Iron(5)
        ];

        GoodsList actualInventory = inventory - input;

        Assert.Equal(expectedInventory, actualInventory);
    }

    [Fact]
    public void AddGoodsList()
    {
        GoodsList inventory =
        [
            new Iron(15)
        ];

        GoodsList input =
        [
            new Iron(10)
        ];

        GoodsList expectedInventory = [
            new Iron(25)
        ];

        GoodsList actualInventory = inventory + input;

        Assert.Equal(expectedInventory, actualInventory);
    }
}
