using InvictaDB;

namespace StellaInvicta;

/// <summary>
/// Represents a system within the game.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// The name of the system.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The description of the system.
    /// </summary>
    public string Description { get; }
    /// <summary>
    /// The author of the system.
    /// </summary>
    public string Author { get; }
    /// <summary>
    /// Indicates whether the system is enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Indicates whether the system has been initialized.
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Initializes the system.
    /// </summary>
    /// <param name="db"></param>
    public void Initialize(InvictaDatabase db);

    /// <summary>
    /// Shuts down the system.
    /// </summary>
    public void Shutdown(InvictaDatabase db);

    /// <summary>
    /// Runs the system logic. Returns an updated database.
    /// </summary>
    /// <param name="db"></param>
    public InvictaDatabase Run(InvictaDatabase db);
}