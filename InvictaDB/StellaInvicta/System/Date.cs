using InvictaDB;
using InvictaDB.Messaging;
using static InvictaDB.Messaging.GameMessages;

namespace StellaInvicta.System.Date;

/// <summary>
/// Manages in-game date and time.
/// </summary>
public class DateSystem : ISystem
{
    /// <inheritdoc/>
    public string Name => DateSystemSender;

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
    public InvictaDatabase Initialize(InvictaDatabase db)
    {
        IsInitialized = true;
        return db.InsertSingleton(new DateTime(1, 1, 1, 0, 0, 0));
    }
    /// <inheritdoc/>
    public InvictaDatabase Shutdown(InvictaDatabase db)
    {
        // No specific shutdown logic needed for the date system.
        return db;
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
        db = db.InsertSingleton(newDate);

        // Send time-related messages
        db = SendTimeMessages(db, currentDate, newDate);

        return db;
    }

    /// <summary>
    /// Sends messages for any time boundaries that were crossed.
    /// </summary>
    private InvictaDatabase SendTimeMessages(InvictaDatabase db, DateTime oldDate, DateTime newDate)
    {
        // Always send NewHour message
        db = db.SendMessage(Name, new NewHour(newDate, oldDate));

        // Check if we crossed into a new day
        if (newDate.Date != oldDate.Date)
        {
            db = db.SendMessage(Name, new NewDay(newDate, oldDate));

            // Check for new week (Monday)
            if (newDate.DayOfWeek == DayOfWeek.Monday)
            {
                var weekNumber = GetWeekOfYear(newDate);
                db = db.SendMessage(Name, new NewWeek(newDate, weekNumber));
            }
        }

        // Check if we crossed into a new month
        if (newDate.Month != oldDate.Month)
        {
            db = db.SendMessage(Name, new NewMonth(newDate, oldDate.Month));

            // Check for season change (months 3, 6, 9, 12)
            var oldSeason = GetSeason(oldDate);
            var newSeason = GetSeason(newDate);
            if (newSeason != oldSeason)
            {
                db = db.SendMessage(Name, new NewSeason(newDate, newSeason));
            }
        }

        // Check if we crossed into a new year
        if (newDate.Year != oldDate.Year)
        {
            db = db.SendMessage(Name, new NewYear(newDate, oldDate.Year));
        }

        return db;
    }

    /// <summary>
    /// Gets the week number of the year for a given date.
    /// </summary>
    private static int GetWeekOfYear(DateTime date)
    {
        var cal = global::System.Globalization.CultureInfo.InvariantCulture.Calendar;
        return cal.GetWeekOfYear(date, global::System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}