namespace InvictaDB.Messaging;

/// <summary>
/// Common message types for game systems.
/// </summary>
public static class GameMessages
{
    /// <summary>
    /// The sender name for the date system.
    /// </summary>
    public const string DateSystemSender = "Date System";

    /// <summary>
    /// Message sent when a new hour begins.
    /// </summary>
    /// <param name="NewDate">The new date/time.</param>
    /// <param name="PreviousDate">The previous date/time.</param>
    public record NewHour(DateTime NewDate, DateTime PreviousDate);

    /// <summary>
    /// Message sent when a new day begins.
    /// </summary>
    /// <param name="NewDate">The new date.</param>
    /// <param name="PreviousDate">The previous date.</param>
    public record NewDay(DateTime NewDate, DateTime PreviousDate);

    /// <summary>
    /// Message sent when a new week begins (Monday).
    /// </summary>
    /// <param name="NewDate">The new date.</param>
    /// <param name="WeekNumber">The week number in the year.</param>
    public record NewWeek(DateTime NewDate, int WeekNumber);

    /// <summary>
    /// Message sent when a new month begins.
    /// </summary>
    /// <param name="NewDate">The new date.</param>
    /// <param name="PreviousMonth">The previous month number.</param>
    public record NewMonth(DateTime NewDate, int PreviousMonth);

    /// <summary>
    /// Message sent when a new year begins.
    /// </summary>
    /// <param name="NewDate">The new date.</param>
    /// <param name="PreviousYear">The previous year.</param>
    public record NewYear(DateTime NewDate, int PreviousYear);

    /// <summary>
    /// Message sent when a new season begins.
    /// </summary>
    /// <param name="NewDate">The new date.</param>
    /// <param name="Season">The new season.</param>
    public record NewSeason(DateTime NewDate, Season Season);

    /// <summary>
    /// Represents the four seasons.
    /// </summary>
    public enum Season
    {
        /// <summary>Spring (March-May)</summary>
        Spring,
        /// <summary>Summer (June-August)</summary>
        Summer,
        /// <summary>Autumn (September-November)</summary>
        Autumn,
        /// <summary>Winter (December-February)</summary>
        Winter
    }

    /// <summary>
    /// Gets the season for a given date.
    /// </summary>
    public static Season GetSeason(DateTime date)
    {
        return date.Month switch
        {
            12 or 1 or 2 => Season.Winter,
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            _ => Season.Autumn
        };
    }
}
