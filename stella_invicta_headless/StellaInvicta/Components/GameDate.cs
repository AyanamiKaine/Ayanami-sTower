namespace StellaInvicta.Components;

/// <summary>
/// Represents the in-game date for Stella Invicta, similar to Paradox games.
/// Simple GameDate
/// 24 Hour Days
/// 30 Days each month
/// 360 Days each year
/// </summary>
public struct GameDate(int year, int month = 1, int day = 1, int hour = 0, int minute = 0, int turn = 0) : IEquatable<GameDate>
{
    /// <summary>
    /// Gets or sets the year component of the game date.
    /// </summary>
    public int Year { get; set; } = year;
    /// <summary>
    /// Gets or sets the Month component of the game date.
    /// </summary>
    public int Month { get; set; } = month;
    /// <summary>
    /// Gets or sets the Day component of the game date.
    /// </summary>
    public int Day { get; set; } = day;
    /// <summary>
    /// Gets or sets the Hour component of the game date.
    /// </summary>
    public int Hour { get; set; } = hour;
    /// <summary>
    /// Gets or sets the Minute component of the game date.
    /// </summary>
    public int Minute { get; set; } = minute;

    /// <summary>
    /// Gets or sets the Turn component of the game date.
    /// </summary>
    public int Turn { get; set; } = turn;


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
    /// Advances the game date by the specified number of days.
    /// </summary>
    /// <param name="days">The number of days to advance the date by. Defaults to 1 if not specified.</param>
    /// <remarks>
    /// This is a simplified implementation that assumes all months have 30 days.
    /// When the day count exceeds 30, it increments the month and resets the day count accordingly.
    /// Similarly, when the month exceeds 12, it increments the year and resets the month to 1.
    /// </remarks>
    public void AdvanceDay(int days = 1)
    {
        Day += days;
        while (Day > 30) // Simplified - real implementation would account for varying month lengths
        {
            Day -= 30;
            Month++;

            if (Month > 12)
            {
                Month = 1;
                Year++;
            }
        }
    }

    /// <summary>
    /// Advances the game date by the specified number of hours.
    /// </summary>
    /// <param name="hours">The number of hours to advance the time by. Defaults to 1 if not specified.</param>
    /// <remarks>
    /// When the hour count exceeds 23, it increments the day and resets the hour count accordingly.
    /// This will cascade to month and year changes as needed.
    /// </remarks>
    public void AdvanceHour(int hours = 1)
    {
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
    }

    /// <summary>
    /// Advances the game date by the specified number of months.
    /// </summary>
    /// <param name="months">The number of months to advance the date by. Defaults to 1 if not specified.</param>
    /// <remarks>
    /// When the month count exceeds 12, it increments the year and resets the month count accordingly.
    /// </remarks>
    public void AdvanceMonth(int months = 1)
    {
        Month += months;

        while (Month > 12)
        {
            Month -= 12;
            Year++;
        }
    }

    /// <summary>
    /// Advances the game date by the specified number of years.
    /// </summary>
    /// <param name="years">The number of years to advance the date by. Defaults to 1 if not specified.</param>
    public void AdvanceYear(int years = 1)
    {
        Year += years;
    }

    /// <summary>
    /// Advances the game by the specified number of turns.
    /// </summary>
    /// <param name="turns">The number of turns to advance. Defaults to 1 if not specified.</param>
    /// <param name="daysPerTurn">The number of days each turn represents. Defaults to 30 (one month).</param>
    /// <remarks>
    /// This allows for flexible turn-based gameplay that advances the calendar accordingly.
    /// </remarks>
    public void AdvanceTurn(int turns = 1, int daysPerTurn = 30)
    {
        Turn += turns;
        AdvanceDay(turns * daysPerTurn);
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