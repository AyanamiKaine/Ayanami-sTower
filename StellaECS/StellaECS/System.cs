namespace StellaECS;

/// <summary>
/// Represents a system.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Executes the system's logic.
    /// </summary>
    public void Run();
}