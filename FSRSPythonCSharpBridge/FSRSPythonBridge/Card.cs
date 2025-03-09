using System.Globalization;
using Python.Runtime;
using NLog;

namespace FSRSPythonBridge;


/// <summary>
/// Represents the current state of a card used for the scheduler.
/// </summary>
public enum CardState
{
    /// <summary>
    /// (==1) new card being studied for the first time
    /// </summary>
    Learning = 1,
    /// <summary>
    /// (==2) card that has "graduated" from the Learning state
    /// </summary>
    Review = 2,
    /// <summary>
    /// (==3) card that has "lapsed" from the Review state
    /// </summary>
    Relearning = 3,
}

/// <summary>
/// Represents a card used for spaced repetition
/// </summary>
/// <param name="card"></param>
public class Card(dynamic card)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The card's current learning state.
    /// </summary>
    public CardState State
    {
        get
        {
            using (Py.GIL())
            {
                int state = PyObject.state.As<int>();
                Logger.Debug("Getting card state: {State}", (CardState)state);
                return (CardState)state;
            }
        }
        set
        {
            using (Py.GIL())
            {
                Logger.Debug("Setting card state from {OldState} to {NewState}",
                    PyObject.state.As<int>(), (int)value);
                PyObject.state = ((int)value).ToPython();
            }
        }
    }
    /// <summary>
    /// Returns the underlying pyobject HANDLE WITH CARE!.
    /// SHOULD ONLY BE USED IN A PYTHON CONTEXT
    /// </summary>
    public dynamic PyObject { get; } = card;
    /// <summary>
    /// The id of the card. Defaults to the epoch miliseconds of when the card was created.
    /// </summary>
    public long ID
    {
        get
        {
            using (Py.GIL())
            {
                long id = PyObject.card_id.As<long>();
                Logger.Trace("Getting card ID: {ID}", id);
                return id;
            }
        }
        set
        {
            using (Py.GIL())
            {
                Logger.Debug("Setting card ID from {OldID} to {NewID}",
                    PyObject.card_id.As<long>(), value);
                PyObject.card_id = value.ToPython();
            }
        }
    }
    /// <summary>
    /// The card's current learning or relearning step or None if the card is in the Review state.
    /// </summary>
    public long? Step
    {
        get
        {
            using (Py.GIL())
            {
                if (PyObject.step == null || PyObject.step.IsNone())
                {
                    Logger.Trace("Getting card step: null");
                    return null;
                }
                long step = PyObject.step.As<long>();
                Logger.Trace("Getting card step: {Step}", step);
                return step;
            }
        }
        set
        {
            using (Py.GIL())
            {
                var oldStep = PyObject.step == null || PyObject.step.IsNone() ? "null" : PyObject.step.As<long>().ToString();
                Logger.Debug("Setting card step from {OldStep} to {NewStep}", oldStep, value);
                PyObject.step = value.ToPython();
            }
        }
    }
    /// <summary>
    /// The date and time when the card is due next.
    /// </summary>
    public DateTime Due
    {
        get
        {
            using (Py.GIL())
            {
                try
                {
                    string dueString = PyObject.due.ToString();
                    DateTime parsedDateTime;

                    // Try parsing with the first format (with fractional seconds)
                    if (DateTime.TryParseExact(dueString, "yyyy-MM-dd HH:mm:ss.ffffffzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                    {
                        Logger.Trace("Getting card due date: {Due}", parsedDateTime);
                        return parsedDateTime;
                    }
                    // Try parsing with the second format (without fractional seconds)
                    else if (DateTime.TryParseExact(dueString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                    {
                        Logger.Trace("Getting card due date: {Due}", parsedDateTime);
                        return parsedDateTime;
                    }
                    else
                    {
                        var ex = new FormatException($"Could not parse '{dueString}' as a valid DateTime with either of the expected formats.");
                        Logger.Error(ex, "Failed to parse due date '{DueString}'", dueString);
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error getting card due date");
                    throw;
                }
            }
        }
        set
        {
            using (Py.GIL())
            {
                try
                {
                    Logger.Debug("Setting card due date to {Due}", value);
                    // Convert C# DateTime to Python datetime object
                    PyObject.due = ToPythonDateTime(value);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error setting card due date to {Due}", value);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// The date and time of the card's last review.
    /// </summary>
    public DateTime? LastReview
    {
        get
        {
            using (Py.GIL())
            {
                try
                {
                    if (PyObject.last_review == null || PyObject.last_review.IsNone())
                    {
                        Logger.Trace("Getting card last review: null");
                        return null;
                    }
                    string lastReviewString = PyObject.last_review.ToString();
                    DateTime parsedDateTime;

                    // Try parsing with the first format (with fractional seconds)
                    if (DateTime.TryParseExact(lastReviewString, "yyyy-MM-dd HH:mm:ss.ffffffzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                    {
                        Logger.Trace("Getting card last review: {LastReview}", parsedDateTime);
                        return parsedDateTime;
                    }
                    // Try parsing with the second format (without fractional seconds)
                    else if (DateTime.TryParseExact(lastReviewString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                    {
                        Logger.Trace("Getting card last review: {LastReview}", parsedDateTime);
                        return parsedDateTime;
                    }
                    else
                    {
                        var ex = new FormatException($"Could not parse '{lastReviewString}' as a valid DateTime with either of the expected formats.");
                        Logger.Error(ex, "Failed to parse last review date '{LastReviewString}'", lastReviewString);
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error getting card last review date");
                    throw;
                }
            }
        }
        set
        {
            using (Py.GIL())
            {
                try
                {
                    Logger.Debug("Setting card last review to {LastReview}", value);
                    if (value.HasValue)
                    {
                        // Convert C# DateTime to Python datetime object
                        PyObject pyDateTime = ToPythonDateTime(value.Value);
                        PyObject.last_review = pyDateTime;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error setting card last review date to {LastReview}", value);
                    throw;
                }
            }
        }
    }

    /// <summary>
    ///  Core mathematical parameter used for future scheduling.
    /// </summary>
    public float? Stability
    {
        get
        {
            using (Py.GIL())
            {
                try
                {
                    if (PyObject.stability == null || PyObject.stability.IsNone())
                    {
                        Logger.Trace("Getting card stability: null");
                        return null;
                    }

                    float stability = PyObject.stability.As<float>();
                    Logger.Trace("Getting card stability: {Stability}", stability);
                    return stability;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error getting card stability");
                    throw;
                }
            }
        }
        set
        {
            using (Py.GIL())
            {
                try
                {
                    var oldStability = PyObject.stability == null || PyObject.stability.IsNone() ? "null" : PyObject.stability.As<float>().ToString(CultureInfo.InvariantCulture);
                    Logger.Debug("Setting card stability from {OldStability} to {NewStability}", oldStability, value);
                    PyObject.stability = value.ToPython();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error setting card stability to {Stability}", value);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Core mathematical parameter used for future scheduling.
    /// </summary>
    public float? Difficulty
    {
        get
        {
            using (Py.GIL())
            {
                try
                {
                    if (PyObject.difficulty == null || PyObject.difficulty.IsNone())
                    {
                        Logger.Trace("Getting card difficulty: null");
                        return null;
                    }

                    float difficulty = PyObject.difficulty.As<float>();
                    Logger.Trace("Getting card difficulty: {Difficulty}", difficulty);
                    return difficulty;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error getting card difficulty");
                    throw;
                }
            }
        }
        set
        {
            using (Py.GIL())
            {
                try
                {
                    var oldDifficulty = PyObject.difficulty == null || PyObject.difficulty.IsNone() ? "null" : PyObject.difficulty.As<float>().ToString(CultureInfo.InvariantCulture);
                    Logger.Debug("Setting card difficulty from {OldDifficulty} to {NewDifficulty}", oldDifficulty, value);
                    PyObject.difficulty = value.ToPython();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error setting card difficulty to {Difficulty}", value);
                    throw;
                }
            }
        }
    }
    // Helper function to convert C# DateTime to Python datetime
    private PyObject ToPythonDateTime(DateTime dt)
    {
        using (Py.GIL())
        {
            try
            {
                // Ensure the DateTime is in UTC
                if (dt.Kind != DateTimeKind.Utc)
                {
                    dt = dt.ToUniversalTime();
                    Logger.Trace("Converted DateTime to UTC: {DateTime}", dt);
                }

                // Import the Python datetime module
                PyObject pyDateTimeModule = Py.Import("datetime");
                PyObject pyDateTimeType = pyDateTimeModule.GetAttr("datetime");
                PyObject pyUtc = pyDateTimeModule.GetAttr("timezone").GetAttr("utc");

                // Create an aware Python datetime object in UTC
                var result = pyDateTimeType.Invoke(
                    dt.Year.ToPython(),
                    dt.Month.ToPython(),
                    dt.Day.ToPython(),
                    dt.Hour.ToPython(),
                    dt.Minute.ToPython(),
                    dt.Second.ToPython(),
                    (dt.Millisecond * 1000).ToPython(), // Python uses microseconds
                    pyUtc
                );

                Logger.Trace("Converted to Python DateTime: {PyDateTime}", result.ToString());
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error converting DateTime {DateTime} to Python datetime", dt);
                throw;
            }
        }
    }
}