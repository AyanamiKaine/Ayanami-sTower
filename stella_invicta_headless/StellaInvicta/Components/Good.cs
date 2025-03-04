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
    int Quantity { get; }
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

/// <summary>
/// Abstract base class for all goods
/// </summary>
/// <param name="goodId"></param>
/// <param name="quantity"></param>
public abstract class Good(string goodId, int quantity) : IGood
{
    /// <inheritdoc/>
    public int Quantity { get; } = Math.Max(0, quantity);
    /// <inheritdoc/>
    public string GoodId { get; } = goodId;
    /// <inheritdoc/>
    public virtual bool IsDefined => true;

    /// <inheritdoc/>
    public abstract IGood WithQuantity(int newQuantity);

    /// <summary>
    /// + operator for adding quantities of the same good type
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Good operator +(Good a, Good b)
    {
        if (a.GoodId != b.GoodId)
            throw new InvalidOperationException($"Cannot add goods of different types: {a.GoodId} and {b.GoodId}");

        return (Good)a.WithQuantity(a.Quantity + b.Quantity);
    }
}