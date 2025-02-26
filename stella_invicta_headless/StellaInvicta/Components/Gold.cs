namespace StellaInvicta.Components;
/// <summary>
/// Finite resource used for many things
/// </summary>
/// <param name="Quantity"></param>
public record struct Gold(int Quantity) : IResource
{
    /// <inheritdoc/>
    public void Add(int value)
    {
        Quantity += value;
    }

    /// <inheritdoc/>
    public int Subtract(int value)
    {
        var subtractedAmount = Math.Max(0, Quantity - value);
        Quantity = subtractedAmount;
        return subtractedAmount;
    }
}
