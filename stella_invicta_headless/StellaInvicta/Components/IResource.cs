namespace StellaInvicta.Components;

/// <summary>
/// Represents a resource that can be added to or subtracted from.
/// </summary>
/// <remarks>
/// This interface defines basic operations for managing numeric resources in the game.
/// </remarks>
public interface IResource
{
    /// <summary>
    /// Adds the specified value to the resource.
    /// </summary>
    /// <param name="value">The amount to add to the resource.</param>
    public void Add(int value);
    /// <summary>
    /// Subtracts the specified value to the resource.
    /// </summary>
    /// <param name="value">The amount to add to the resource.</param>
    public int Subtract(int value);
}