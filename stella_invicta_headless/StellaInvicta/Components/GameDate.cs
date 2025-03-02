namespace StellaInvicta.Components;

/// <summary>
/// Represents the in-game date for Stella Invicta, similar to Paradox games.
/// Simple GameDate
/// 24 Hour Days
/// 30 Days each month
/// 360 Days each year
/// </summary>
public struct GameDate : IEquatable<GameDate>
{
    // Define delegate types for our events
    /// <summary>
    /// Delegate for day change events, providing the old and new date.
    /// </summary>
    public delegate void DayChangedEventHandler(GameDate oldDate, GameDate newDate);

    /// <summary>
    /// Delegate for month change events, providing the old and new date.
    /// </summary>
    public delegate void MonthChangedEventHandler(GameDate oldDate, GameDate newDate);

    /// <summary>
    /// Delegate for year change events, providing the old and new date.
    /// </summary>
    public delegate void YearChangedEventHandler(GameDate oldDate, GameDate newDate);

    /// <summary>
    /// Delegate for turn change events, providing the old and new date.
    /// </summary>
    public delegate void TurnChangedEventHandler(GameDate oldDate, GameDate newDate, int turnsPassed);

    // Static events that subscribers can register with
    /// <summary>
    /// Event that fires when a day changes.
    /// </summary>
    public static event DayChangedEventHandler? DayChanged;

    /// <summary>
    /// Event that fires when a month changes.
    /// </summary>
    public static event MonthChangedEventHandler? MonthChanged;

    /// <summary>
    /// Event that fires when a year changes.
    /// </summary>
    public static event YearChangedEventHandler? YearChanged;

    /// <summary>
    /// Event that fires when a turn changes.
    /// </summary>
    public static event TurnChangedEventHandler? TurnChanged;

    /// <summary>
    /// Gets or sets the year component of the game date.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the Month component of the game date.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the Day component of the game date.
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Gets or sets the Hour component of the game date.
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Gets or sets the Minute component of the game date.
    /// </summary>
    public int Minute { get; set; }

    /// <summary>
    /// Gets or sets the Turn component of the game date.
    /// </summary>
    public int Turn { get; set; }

    /// <summary>
    /// Initializes a new instance of the GameDate struct.
    /// </summary>
    public GameDate(int year, int month = 1, int day = 1, int hour = 0, int minute = 0, int turn = 0)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Turn = turn;
    }

    /// <summary>
    /// Returns the date in a format suitable for space 4X display.
    /// </summary>
    /// <returns>A string representation of the star date.</returns>
    public readonly string GetStarDateFormat()
    {
        // Popular sci-fi style format: [Year].[Fraction of Year]
        double yearFraction = ((Month - 1) * 30) + Day;
        double fraction = yearFraction / 360.0; // Assuming 360-day year
        return $"{Year}.{fraction:F2}";
    }

    /// <summary>
    /// Returns the game date in a formatted string.
    /// </summary>
    /// <returns>A string representation of the date in the format "YYYY.MM.DD" where MM and DD are zero-padded.</returns>
    public readonly string GetFormattedDate() => $"{Year}.{Month:D2}.{Day:D2}";

    /// <summary>
    /// Advances the game date by the specified number of days, triggering appropriate events.
    /// </summary>
    /// <param name="days">The number of days to advance the date by. Defaults to 1 if not specified.</param>
    public void AdvanceDay(int days = 1)
    {
        // Store the original date for event callbacks
        GameDate oldDate = this;

        Day += days;
        bool monthChanged = false;
        bool yearChanged = false;

        while (Day > 30) // Simplified - real implementation would account for varying month lengths
        {
            Day -= 30;
            Month++;
            monthChanged = true;

            if (Month > 12)
            {
                Month = 1;
                Year++;
                yearChanged = true;
            }
        }

        // Trigger events in proper order: day, month, year
        DayChanged?.Invoke(oldDate, this);

        if (monthChanged)
        {
            MonthChanged?.Invoke(oldDate, this);
        }

        if (yearChanged)
        {
            YearChanged?.Invoke(oldDate, this);
        }
    }

    /// <summary>
    /// Advances the game date by the specified number of hours, triggering appropriate events.
    /// </summary>
    /// <param name="hours">The number of hours to advance the time by. Defaults to 1 if not specified.</param>
    public void AdvanceHour(int hours = 1)
    {
        // Store the original date for event callbacks
        GameDate oldDate = this;

        Hour += hours;
        int daysToAdvance = 0;

        // Calculate full days and remaining hours
        if (Hour >= 24)
        {
            daysToAdvance = Hour / 24;
            Hour %= 24;
        }

        // Advance days if needed
        if (daysToAdvance > 0)
        {
            AdvanceDay(daysToAdvance);
        }
        else
        {
            // If no days were advanced but hours were, we still trigger a day change event
            // This allows for hourly updates without requiring a full day to pass
            DayChanged?.Invoke(oldDate, this);
        }
    }

    /// <summary>
    /// Advances the game date by the specified number of months, triggering appropriate events.
    /// </summary>
    /// <param name="months">The number of months to advance the date by. Defaults to 1 if not specified.</param>
    public void AdvanceMonth(int months = 1)
    {
        // Store the original date for event callbacks
        GameDate oldDate = this;

        Month += months;
        bool yearChanged = false;

        while (Month > 12)
        {
            Month -= 12;
            Year++;
            yearChanged = true;
        }

        // Trigger the month changed event
        MonthChanged?.Invoke(oldDate, this);

        // Trigger the year changed event if applicable
        if (yearChanged)
        {
            YearChanged?.Invoke(oldDate, this);
        }
    }

    /// <summary>
    /// Advances the game date by the specified number of years, triggering appropriate events.
    /// </summary>
    /// <param name="years">The number of years to advance the date by. Defaults to 1 if not specified.</param>
    public void AdvanceYear(int years = 1)
    {
        // Store the original date for event callbacks
        GameDate oldDate = this;

        Year += years;

        // Trigger the year changed event
        YearChanged?.Invoke(oldDate, this);
    }

    /// <summary>
    /// Advances the game by the specified number of turns, triggering appropriate events.
    /// </summary>
    /// <param name="turns">The number of turns to advance. Defaults to 1 if not specified.</param>
    /// <param name="daysPerTurn">The number of days each turn represents. Defaults to 30 (one month).</param>
    public void AdvanceTurn(int turns = 1, int daysPerTurn = 30)
    {
        // Store the original date and turn for event callbacks
        GameDate oldDate = this;
        int oldTurn = Turn;

        Turn += turns;

        // Advance the days
        AdvanceDay(turns * daysPerTurn);

        // Trigger the turn changed event
        TurnChanged?.Invoke(oldDate, this, Turn - oldTurn);
    }

    /// <summary>
    /// Determines whether this GameDate instance is equal to another GameDate instance.
    /// </summary>
    /// <param name="other">The GameDate to compare with this instance.</param>
    /// <returns>true if the specified GameDate is equal to this instance; otherwise, false.</returns>
    public readonly bool Equals(GameDate other)
    {
        return Year == other.Year &&
               Month == other.Month &&
               Day == other.Day &&
               Hour == other.Hour &&
               Minute == other.Minute &&
               Turn == other.Turn;
    }

    /// <summary>
    /// Determines whether this GameDate instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>true if the specified object is a GameDate and equal to this instance; otherwise, false.</returns>
    public override readonly bool Equals(object? obj)
    {
        return obj is GameDate date && Equals(date);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures.</returns>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Year, Month, Day, Hour, Minute, Turn);
    }

    /// <summary>
    /// Determines whether two GameDate instances are equal.
    /// </summary>
    public static bool operator ==(GameDate left, GameDate right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two GameDate instances are not equal.
    /// </summary>
    public static bool operator !=(GameDate left, GameDate right)
    {
        return !left.Equals(right);
    }
}