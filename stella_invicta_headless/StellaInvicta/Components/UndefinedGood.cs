namespace StellaInvicta.Components;
/// <summary>
/// Singleton UndefinedGood implementation
/// </summary>
public sealed class UndefinedGood : Good, IGood
{
    /// <summary>
    /// Gets the singleton instance of the UndefinedGood class.
    /// </summary>
    /// <value>
    /// The singleton instance of UndefinedGood.
    /// </value>
    public static UndefinedGood Instance { get; } = new UndefinedGood();

    /// <summary>
    /// Gets a value indicating whether this good is defined.
    /// Always returns false for UndefinedGood.
    /// </summary>
    public override bool IsDefined => false;

    private UndefinedGood() : base("undefined", 0) { }

    /// <inheritdoc/>
    IGood IGood.WithQuantity(int newQuantity)
    {
        // UndefinedGood always has quantity 0, regardless of attempts to change it
        return Instance;
    }
}