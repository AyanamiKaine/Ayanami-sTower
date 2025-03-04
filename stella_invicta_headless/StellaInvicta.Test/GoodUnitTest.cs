using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;


// Base interface for all goods
public interface IGood
{
    int Quantity { get; }
    Type GoodType { get; } // Used to identify the type of good
}

// Abstract base class implementing IGood
public abstract class Good(int quantity) : IGood
{
    public int Quantity { get; } = Math.Max(0, quantity);
    public Type GoodType => GetType();
}

// Concrete implementation for Coal
public class Coal(int quantity) : Good(quantity)
{
}

// Concrete implementation for Iron
public class Iron(int quantity) : Good(quantity)
{
}

// Custom collection class that inherits from List<IGood> and adds operator overloading
public class GoodsList : List<IGood>
{
    public GoodsList() { }

    public GoodsList(IEnumerable<IGood> collection) : base(collection) { }

    // Operator to check if inventory has enough goods for input
    public static bool operator >=(GoodsList inventory, GoodsList input)
    {
        // Group inventory by type and sum quantities
        var inventoryDict = inventory.GroupBy(i => i.GoodType)
                                   .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        // Check each input item against inventory
        foreach (var inputItem in input)
        {
            // If type not found in inventory or not enough quantity, return false
            if (!inventoryDict.TryGetValue(inputItem.GoodType, out int availableQuantity) ||
                availableQuantity < inputItem.Quantity)
            {
                return false;
            }
        }

        return true;
    }

    // Reversed operator for convenience
    public static bool operator <=(GoodsList input, GoodsList inventory)
    {
        return inventory >= input;
    }
}

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
            new Coal(20),
            new Iron(15)
        ];

        GoodsList input =
        [
            new Coal(5),
            new Iron(10)
        ];

        Assert.True(inventory >= input);
    }
}
