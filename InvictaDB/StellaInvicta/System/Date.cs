using InvictaDB;

namespace StellaInvicta.System.Date;

/// <summary>
/// Manages in-game date and time.
/// </summary>
public class DateSystem : ISystem
{
    /// <inheritdoc/>
    public string Name => "Date System";

    /// <inheritdoc/>
    public string Description => "Manages in-game date and time.";
    /// <inheritdoc/>
    public string Author => "InvictaDB Team";
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Indicates whether the system has been initialized.
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <inheritdoc/>
    public void Initialize(InvictaDatabase db)
    {
        db.InsertSingleton(new DateTime(1, 1, 1, 0, 0, 0));
        IsInitialized = true;
    }
    /// <inheritdoc/>
    public void Shutdown(InvictaDatabase db)
    {
        // No specific shutdown logic needed for the date system.
    }
    /// <summary>
    /// Advances the in-game date by one hour.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public InvictaDatabase Run(InvictaDatabase db)
    {
        // Get the current game date
        var currentDate = db.GetSingleton<DateTime>();

        // Advance the date by one hour
        var newDate = currentDate.AddHours(1);

        // Update the database with the new date
        return db.InsertSingleton(newDate);
    }
}