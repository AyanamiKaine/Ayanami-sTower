namespace StellaInvicta.Components;
/// <summary>
/// Iron good, used for basic construction
/// </summary>
public class Iron(int quantity) : Good("Iron", quantity)
{
    /// <inheritdoc/>
    public override IGood WithQuantity(int newQuantity)
    {
        return new Iron(newQuantity);
    }
}