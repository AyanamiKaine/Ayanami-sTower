namespace StellaInvicta.Components;
/// <summary>
/// Base interface for goods
/// </summary>
public interface IGood
{
    /// <summary>
    /// Gets the quantity of the good.
    /// </summary>
    /// <value>The current quantity of the good.</value>
    int Quantity { get; set; }
    /// <summary>
    /// Gets the unique identifier for the good.
    /// </summary>
    /// <value>A string representing the good's unique identifier.</value>
    string GoodId { get; }
    /// <summary>
    /// Helper to check if this is a defined good
    /// </summary>
    bool IsDefined { get; }
    /// <summary>
    /// Method to create a new instance with modified quantity
    /// </summary>
    /// <param name="newQuantity"></param>
    /// <returns></returns>
    IGood WithQuantity(int newQuantity);
}

/*NOTE:
This was once implemented as an immutable type, sadly the performance hit in various critical loops
simply resulted in too many allocations, such much that most time was spend collecting and allocating.

While I really like immutable types in this case they where problamatic
*/

/// <summary>
/// Abstract base class for all goods
/// </summary>
/// <param name="goodId"></param>
/// <param name="quantity"></param>
public abstract class Good(string goodId, int quantity) : IGood
{
    /// <inheritdoc/>
    public int Quantity { get; set; } = Math.Max(0, quantity);
    /// <inheritdoc/>
    public string GoodId { get; } = goodId;
    /// <inheritdoc/>
    public virtual bool IsDefined => true;

    /// <summary>
    /// Sets a new quantity and returns this instance for method chaining
    /// </summary>
    public virtual IGood WithQuantity(int newQuantity)
    {
        Quantity = Math.Max(0, newQuantity);
        return this;
    }

    /// <summary>
    /// + operator for adding quantities of the same good type
    /// </summary>
    /// <param name="a">First good (will be modified)</param>
    /// <param name="b">Second good</param>
    /// <returns>The modified first good</returns>
    public static Good operator +(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot add goods of different types: {a.GoodId} and {b.GoodId}");

        a.Quantity += b.Quantity;
        return a;
    }
    /// <summary>
    /// - operator for subtracting quantities of the same good type
    /// </summary>
    /// <param name="a">The good to subtract from (will be modified)</param>
    /// <param name="b">The good to subtract</param>
    /// <returns>The modified first good</returns>
    public static Good operator -(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot subtract goods of different types: {a.GoodId} and {b.GoodId}");

        a.Quantity = Math.Max(0, a.Quantity - b.Quantity);
        return a;
    }

    /// <summary>
    /// == operator for comparing equality of goods
    /// </summary>
    /// <param name="a">First good</param>
    /// <param name="b">Second good</param>
    /// <returns>True if goods have the same ID and quantity</returns>
    public static bool operator ==(Good a, Good b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        return a.GoodId == b.GoodId && a.Quantity == b.Quantity;
    }

    /// <summary>
    /// != operator for comparing inequality of goods
    /// </summary>
    /// <param name="a">First good</param>
    /// <param name="b">Second good</param>
    /// <returns>True if goods have different ID or quantity</returns>
    public static bool operator !=(Good a, Good b) => !(a == b);

    /// <summary>
    /// > operator for comparing quantities of goods
    /// </summary>
    /// <param name="a">First good</param>
    /// <param name="b">Second good</param>
    /// <returns>True if the first good has a greater quantity than the second</returns>
    /// <exception cref="InvalidOperationException">Thrown when goods have different IDs</exception>
    public static bool operator >(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot compare goods of different types: {a.GoodId} and {b.GoodId}");

        return a.Quantity > b.Quantity;
    }

    /// <summary>
    /// operator for comparing quantities of goods
    /// </summary>
    /// <param name="a">First good</param>
    /// <param name="b">Second good</param>
    /// <returns>True if the first good has a lesser quantity than the second</returns>
    /// <exception cref="InvalidOperationException">Thrown when goods have different IDs</exception>
    public static bool operator <(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot compare goods of different types: {a.GoodId} and {b.GoodId}");

        return a.Quantity < b.Quantity;
    }

    /// <summary>
    /// >= operator for comparing quantities of goods
    /// </summary>
    /// <param name="a">First good</param>
    /// <param name="b">Second good</param>
    /// <returns>True if the first good has a greater or equal quantity than the second</returns>
    /// <exception cref="InvalidOperationException">Thrown when goods have different IDs</exception>
    public static bool operator >=(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot compare goods of different types: {a.GoodId} and {b.GoodId}");

        return a.Quantity >= b.Quantity;
    }

    /// <summary>
    /// lesser or equal operator for comparing quantities of goods
    /// </summary>
    /// <param name="a">First good</param>
    /// <param name="b">Second good</param>
    /// <returns>True if the first good has a lesser or equal quantity than the second</returns>
    /// <exception cref="InvalidOperationException">Thrown when goods have different IDs</exception>
    public static bool operator <=(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot compare goods of different types: {a.GoodId} and {b.GoodId}");

        return a.Quantity <= b.Quantity;
    }

    /// <summary>
    /// Override Equals to be consistent with == operator
    /// </summary>
    /// <param name="obj">Object to compare</param>
    /// <returns>True if objects are equal</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Good other)
        {
            return this == other;
        }
        return false;
    }

    /// <summary>
    /// Override GetHashCode to be consistent with Equals
    /// </summary>
    /// <returns>Hash code for the good</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(GoodId, Quantity);
    }
}