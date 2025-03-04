using System.Collections;

namespace StellaInvicta.Components;
/// <summary>
/// Represents a collection of goods with efficient lookup and quantity management capabilities.
/// </summary>
/// <remarks>
/// The GoodsList class provides O(1) lookup performance using an internal dictionary structure.
/// It handles quantity aggregation automatically when adding goods with the same ID.
/// </remarks>
/// <example>
/// <code>
/// var goodsList = new GoodsList();
/// goodsList.Add(new Good("wood", 5));
/// goodsList.Add(new Good("wood", 3)); // Results in 8 wood
/// var woodQuantity = goodsList.GetQuantity("wood"); // Returns 8
/// </code>
/// </example>
public class GoodsList : IEnumerable<IGood>
{
    // Using a dictionary for O(1) lookups
    private readonly Dictionary<string, IGood> _goods = [];

    /// <summary>
    /// Empty default constructor
    /// </summary>
    public GoodsList() { }

    /// <summary>
    /// Constructor that accepts IEnumerable IGood for collection expressions
    /// </summary>
    /// <param name="goods"></param>
    public GoodsList(IEnumerable<IGood> goods)
    {
        if (goods != null)
        {
            foreach (var good in goods)
            {
                Add(good);
            }
        }
    }

    /// <summary>
    /// Adds a good to the goods list. If the good already exists, its quantity is increased.
    /// </summary>
    /// <param name="good">The good to be added to the list.</param>
    /// <remarks>
    /// If a good with the same ID already exists in the list, the quantities are combined.
    /// If the good doesn't exist, it is added as a new entry.
    /// </remarks>
    public void Add(IGood good)
    {
        if (_goods.TryGetValue(good.GoodId, out IGood? existingGood))
        {
            _goods[good.GoodId] = existingGood.WithQuantity(existingGood.Quantity + good.Quantity);
        }
        else
        {
            _goods[good.GoodId] = good;
        }
    }



    /// <summary>
    /// Adds a collection of goods to the goods list.
    /// </summary>
    /// <param name="goods">The collection of goods to be added.</param>
    /// <remarks>
    /// Each good in the collection is added individually using the Add method.
    /// </remarks>
    public void AddRange(IEnumerable<IGood> goods)
    {
        foreach (var good in goods)
            Add(good);
    }
    /// <summary>
    /// Retrieves a good from the collection by its ID.
    /// </summary>
    /// <param name="goodId">The unique identifier of the good to retrieve.</param>
    /// <returns>The good if found; otherwise returns an UndefinedGood instance.</returns>
    public int GetQuantity(string goodId)
    {
        return _goods.TryGetValue(goodId, out IGood? good) ? good.Quantity : 0;
    }
    /// <summary>
    /// Retrieves a good from the goods list by its ID.
    /// </summary>
    /// <param name="goodId">The unique identifier of the good to retrieve.</param>
    /// <returns>The requested good if found; otherwise returns an UndefinedGood instance.</returns>
    public IGood GetGood(string goodId)
    {
        // I would like to return a default UNDEFINEDGOOD good type
        return _goods.TryGetValue(goodId, out IGood? good) ? good : UndefinedGood.Instance;
    }

    /// <summary>
    /// Return all goods in the list
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IGood> GetAllGoods()
    {
        return _goods.Values;
    }

    /// <inheritdoc/>
    public IEnumerator<IGood> GetEnumerator()
    {
        return _goods.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// + operator to combine two GoodsLists
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static GoodsList operator +(GoodsList a, GoodsList b)
    {
        return [
            // Add all goods from first list
            .. a._goods.Values,
            // Add all goods from second list
            .. b._goods.Values,
        ];
    }
    /// <summary>
    /// Add operator overloads for GoodsList + IGood
    /// </summary>
    /// <param name="list"></param>
    /// <param name="good"></param>
    /// <returns></returns>
    public static GoodsList operator +(GoodsList list, IGood good)
    {
        GoodsList result = new();

        // First add all existing goods
        result.AddRange(list._goods.Values);

        // Then add the new good
        result.Add(good);

        return result;
    }

    /// <summary>
    /// Add the reverse operator for Good + GoodsList
    /// </summary>
    /// <param name="good"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static GoodsList operator +(IGood good, GoodsList list)
    {
        return list + good; // Reuse the implementation above
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    /// <param name="inventory"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool operator >=(GoodsList inventory, GoodsList input)
    {
        foreach (var good in input._goods.Values)
        {
            if (inventory.GetQuantity(good.GoodId) < good.Quantity)
                return false;
        }
        return true;
    }
    /// <summary>
    /// Determines whether the first GoodsList is less than or equal to the second GoodsList.
    /// </summary>
    /// <param name="input">The first GoodsList to compare.</param>
    /// <param name="inventory">The second GoodsList to compare.</param>
    /// <returns>true if all quantities in the input GoodsList are less than or equal to the corresponding quantities in the inventory GoodsList; otherwise, false.</returns>
    public static bool operator <=(GoodsList input, GoodsList inventory)
    {
        return inventory >= input;
    }
}