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
    public static GoodsList operator +(GoodsList a, GoodsList b)
    {
        // Modify GoodsList a instead of creating a new one
        a.AddRange(b._goods.Values);
        return a;
    }

    /// <summary>
    /// Add operator overloads for GoodsList + IGood
    /// </summary>
    public static GoodsList operator +(GoodsList list, IGood good)
    {
        list.Add(good);
        return list;
    }

    /// <summary>
    /// Add the reverse operator for Good + GoodsList
    /// </summary>
    public static GoodsList operator +(IGood good, GoodsList list)
    {
        return list + good;
    }

    /// <summary>
    /// Subtract one GoodsList from another
    /// </summary>
    public static GoodsList operator -(GoodsList a, GoodsList b)
    {
        // Modify GoodsList a directly
        foreach (var good in b._goods.Values)
        {
            if (a._goods.TryGetValue(good.GoodId, out IGood? existingGood))
            {
                // Subtract directly from the existing good
                existingGood.Quantity = Math.Max(0, existingGood.Quantity - good.Quantity);

                // Remove the good if quantity becomes zero
                if (existingGood.Quantity == 0)
                {
                    a._goods.Remove(good.GoodId);
                }
            }
        }
        return a;
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

    /// <summary>
    /// Determines whether two GoodsList instances are equal.
    /// </summary>
    /// <param name="a">The first GoodsList to compare.</param>
    /// <param name="b">The second GoodsList to compare.</param>
    /// <returns>true if both lists contain the same goods with the same quantities; otherwise, false.</returns>
    public static bool operator ==(GoodsList? a, GoodsList? b)
    {
        // If both are null or the same instance, they're equal
        if (ReferenceEquals(a, b))
            return true;

        // If only one is null, they're not equal
        if (a is null || b is null)
            return false;

        // Check if they have the same number of goods
        if (a._goods.Count != b._goods.Count)
            return false;

        // Check if all goods in a exist in b with the same quantity
        foreach (var pair in a._goods)
        {
            string goodId = pair.Key;
            IGood goodA = pair.Value;

            // If the good doesn't exist in b or has a different quantity, lists are not equal
            if (!b._goods.TryGetValue(goodId, out IGood? goodB) || goodA.Quantity != goodB.Quantity)
                return false;
        }

        // All checks passed, lists are equal
        return true;
    }

    /// <summary>
    /// Determines whether two GoodsList instances are not equal.
    /// </summary>
    /// <param name="a">The first GoodsList to compare.</param>
    /// <param name="b">The second GoodsList to compare.</param>
    /// <returns>true if the lists differ in goods or quantities; otherwise, false.</returns>
    public static bool operator !=(GoodsList? a, GoodsList? b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Determines whether this GoodsList is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current GoodsList.</param>
    /// <returns>true if obj is a GoodsList with the same goods and quantities; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj is GoodsList other && this == other;
    }

    /// <summary>
    /// Returns a hash code for this GoodsList.
    /// </summary>
    /// <returns>A hash code that represents the current GoodsList.</returns>
    public override int GetHashCode()
    {
        int hashCode = 0;

        // Compute hash based on all goods and their quantities
        foreach (var pair in _goods)
        {
            hashCode ^= pair.Key.GetHashCode() ^ pair.Value.Quantity.GetHashCode();
        }

        return hashCode;
    }

    /// <summary>
    /// Determines whether the first GoodsList has strictly greater quantities than the second GoodsList.
    /// </summary>
    /// <param name="a">The first GoodsList to compare.</param>
    /// <param name="b">The second GoodsList to compare.</param>
    /// <returns>
    /// true if all quantities in a are greater than or equal to their counterparts in b, 
    /// and at least one quantity in a is strictly greater than its counterpart in b;
    /// otherwise, false.
    /// </returns>
    public static bool operator >(GoodsList a, GoodsList b)
    {
        bool hasStrictlyGreater = false;

        // First check that all goods in b exist in a with at least equal quantities
        foreach (var good in b._goods.Values)
        {
            int quantityA = a.GetQuantity(good.GoodId);
            int quantityB = good.Quantity;

            // If any quantity in a is less than in b, a is not greater than b
            if (quantityA < quantityB)
                return false;

            // Track if we found at least one strictly greater quantity
            if (quantityA > quantityB)
                hasStrictlyGreater = true;
        }

        // Also check if a has any goods that b doesn't have
        foreach (var good in a._goods.Values)
        {
            if (!b._goods.ContainsKey(good.GoodId) && good.Quantity > 0)
                hasStrictlyGreater = true;
        }

        // a > b if all quantities in a are >= their counterparts in b
        // AND at least one quantity is strictly greater
        return hasStrictlyGreater;
    }

    /// <summary>
    /// Determines whether the first GoodsList has strictly lesser quantities than the second GoodsList.
    /// </summary>
    /// <param name="a">The first GoodsList to compare.</param>
    /// <param name="b">The second GoodsList to compare.</param>
    /// <returns>
    /// true if b has strictly greater quantities than a;
    /// otherwise, false.
    /// </returns>
    public static bool operator <(GoodsList a, GoodsList b)
    {
        return b > a; // Reuse the greater-than implementation with operands swapped
    }
    /// <summary>
    /// Determines whether a GoodsList contains exactly the specified good with the same quantity.
    /// </summary>
    /// <param name="list">The GoodsList to check.</param>
    /// <param name="good">The good to compare against.</param>
    /// <returns>
    /// true if the list contains the good with the exact same quantity and no other goods;
    /// otherwise, false.
    /// </returns>
    public static bool operator ==(GoodsList? list, IGood? good)
    {
        if (list is null)
            return good is null;

        if (good is null)
            return false;

        // List has exactly one good with the same ID and quantity
        return list._goods.Count == 1 &&
            list._goods.TryGetValue(good.GoodId, out IGood? listGood) &&
            listGood.Quantity == good.Quantity;
    }

    /// <summary>
    /// Determines whether a GoodsList does not contain exactly the specified good.
    /// </summary>
    /// <param name="list">The GoodsList to check.</param>
    /// <param name="good">The good to compare against.</param>
    /// <returns>
    /// true if the list does not contain exactly the good with the same quantity or contains other goods;
    /// otherwise, false.
    /// </returns>
    public static bool operator !=(GoodsList? list, IGood? good)
    {
        return !(list == good);
    }

    /// <summary>
    /// Determines whether a good is equal to a GoodsList.
    /// </summary>
    /// <param name="good">The good to compare.</param>
    /// <param name="list">The GoodsList to compare against.</param>
    /// <returns>
    /// true if the list contains exactly the good with the same quantity and no other goods;
    /// otherwise, false.
    /// </returns>
    public static bool operator ==(IGood? good, GoodsList? list)
    {
        return list == good;
    }

    /// <summary>
    /// Determines whether a good is not equal to a GoodsList.
    /// </summary>
    /// <param name="good">The good to compare.</param>
    /// <param name="list">The GoodsList to compare against.</param>
    /// <returns>
    /// true if the list does not contain exactly the good with the same quantity or contains other goods;
    /// otherwise, false.
    /// </returns>
    public static bool operator !=(IGood? good, GoodsList? list)
    {
        return !(good == list);
    }
    /// <summary>
    /// Determines whether a GoodsList contains the specified good with a greater quantity.
    /// </summary>
    /// <param name="list">The GoodsList to check.</param>
    /// <param name="good">The good to compare against.</param>
    /// <returns>
    /// true if the list contains the good with a greater quantity;
    /// otherwise, false.
    /// </returns>
    public static bool operator >(GoodsList? list, IGood? good)
    {
        if (list is null || good is null)
            return false;

        return list.GetQuantity(good.GoodId) > good.Quantity;
    }

    /// <summary>
    /// Determines whether a GoodsList contains the specified good with a lesser quantity.
    /// </summary>
    /// <param name="list">The GoodsList to check.</param>
    /// <param name="good">The good to compare against.</param>
    /// <returns>
    /// true if the list contains the good with a lesser quantity;
    /// otherwise, false.
    /// </returns>
    public static bool operator <(GoodsList? list, IGood? good)
    {
        if (list is null || good is null)
            return false;

        return list.GetQuantity(good.GoodId) < good.Quantity;
    }

    /// <summary>
    /// Determines whether a GoodsList contains the specified good with a greater or equal quantity.
    /// </summary>
    /// <param name="list">The GoodsList to check.</param>
    /// <param name="good">The good to compare against.</param>
    /// <returns>
    /// true if the list contains the good with a greater or equal quantity;
    /// otherwise, false.
    /// </returns>
    public static bool operator >=(GoodsList? list, IGood? good)
    {
        if (list is null)
            return good is null;

        if (good is null)
            return true;

        return list.GetQuantity(good.GoodId) >= good.Quantity;
    }

    /// <summary>
    /// Determines whether a GoodsList contains the specified good with a lesser or equal quantity.
    /// </summary>
    /// <param name="list">The GoodsList to check.</param>
    /// <param name="good">The good to compare against.</param>
    /// <returns>
    /// true if the list contains the good with a lesser or equal quantity;
    /// otherwise, false.
    /// </returns>
    public static bool operator <=(GoodsList? list, IGood? good)
    {
        if (list is null)
            return good is null;

        if (good is null)
            return true;

        return list.GetQuantity(good.GoodId) <= good.Quantity;
    }

    /// <summary>
    /// Determines whether a good has a greater quantity than what's in the GoodsList.
    /// </summary>
    /// <param name="good">The good to compare.</param>
    /// <param name="list">The GoodsList to compare against.</param>
    /// <returns>true if the good's quantity is greater than in the list; otherwise, false.</returns>
    public static bool operator >(IGood? good, GoodsList? list)
    {
        return list < good;
    }

    /// <summary>
    /// Determines whether a good has a lesser quantity than what's in the GoodsList.
    /// </summary>
    /// <param name="good">The good to compare.</param>
    /// <param name="list">The GoodsList to compare against.</param>
    /// <returns>true if the good's quantity is lesser than in the list; otherwise, false.</returns>
    public static bool operator <(IGood? good, GoodsList? list)
    {
        return list > good;
    }

    /// <summary>
    /// Determines whether a good has a greater or equal quantity to what's in the GoodsList.
    /// </summary>
    /// <param name="good">The good to compare.</param>
    /// <param name="list">The GoodsList to compare against.</param>
    /// <returns>true if the good's quantity is greater or equal to the list; otherwise, false.</returns>
    public static bool operator >=(IGood? good, GoodsList? list)
    {
        return list <= good;
    }

    /// <summary>
    /// Determines whether a good has a lesser or equal quantity to what's in the GoodsList.
    /// </summary>
    /// <param name="good">The good to compare.</param>
    /// <param name="list">The GoodsList to compare against.</param>
    /// <returns>true if the good's quantity is lesser or equal to the list; otherwise, false.</returns>
    public static bool operator <=(IGood? good, GoodsList? list)
    {
        return list >= good;
    }
}