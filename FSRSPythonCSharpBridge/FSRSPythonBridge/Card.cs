using System.Globalization;
using Python.Runtime;
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
                return (CardState)state;
            }
        }
        set
        {
            using (Py.GIL())
            {
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
                return PyObject.card_id.As<long>();
            }
        }
        set
        {
            using (Py.GIL())
            {
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
                    return null;
                }
                return PyObject.step.As<long>();
            }
        }
        set
        {
            using (Py.GIL())
            {
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
                string dueString = PyObject.due.ToString();
                DateTime parsedDateTime;

                // Try parsing with the first format (with fractional seconds)
                if (DateTime.TryParseExact(dueString, "yyyy-MM-dd HH:mm:ss.ffffffzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                {
                    return parsedDateTime;
                }
                // Try parsing with the second format (without fractional seconds)
                else if (DateTime.TryParseExact(dueString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                {
                    return parsedDateTime;
                }
                else
                {
                    // Handle the case where neither format matches
                    throw new FormatException($"Could not parse '{dueString}' as a valid DateTime with either of the expected formats.");
                }
            }
        }
        set
        {
            using (Py.GIL())
            {
                // Convert C# DateTime to Python datetime object
                PyObject.due = ToPythonDateTime(value);
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
                if (PyObject.last_review == null || PyObject.last_review.IsNone())
                {
                    return null;
                }
                string lastReviewString = PyObject.last_review.ToString();
                DateTime parsedDateTime;

                // Try parsing with the first format (with fractional seconds)
                if (DateTime.TryParseExact(lastReviewString, "yyyy-MM-dd HH:mm:ss.ffffffzzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                {
                    return parsedDateTime;
                }
                // Try parsing with the second format (without fractional seconds)
                else if (DateTime.TryParseExact(lastReviewString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                {
                    return parsedDateTime;
                }
                else
                {
                    // Handle the case where neither format matches
                    // You can throw an exception, log an error, or return a default value
                    throw new FormatException($"Could not parse '{lastReviewString}' as a valid DateTime with either of the expected formats.");
                }
            }
        }
        set
        {
            using (Py.GIL())
            {
                if (value.HasValue)
                {
                    // Convert C# DateTime to Python datetime object
                    PyObject pyDateTime = ToPythonDateTime(value.Value);
                    PyObject.last_review = pyDateTime;
                }
                else
                {
                    return;
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
                if (PyObject.stability == null || PyObject.stability.IsNone())
                {
                    return null;
                }

                return PyObject.stability.As<float>();
            }
        }
        set
        {
            using (Py.GIL())
            {
                PyObject.stability = value.ToPython();
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
                if (PyObject.difficulty == null || PyObject.difficulty.IsNone())
                {
                    return null;
                }

                return PyObject.difficulty.As<float>();
            }
        }
        set
        {
            using (Py.GIL())
            {
                PyObject.difficulty = value.ToPython();
            }
        }
    }
    // Helper function to convert C# DateTime to Python datetime
    private PyObject ToPythonDateTime(DateTime dt)
    {
        using (Py.GIL())
        {
            // Ensure the DateTime is in UTC
            if (dt.Kind != DateTimeKind.Utc)
            {
                dt = dt.ToUniversalTime();
            }

            // Import the Python datetime module
            PyObject pyDateTimeModule = Py.Import("datetime");
            PyObject pyDateTimeType = pyDateTimeModule.GetAttr("datetime");
            PyObject pyUtc = pyDateTimeModule.GetAttr("timezone").GetAttr("utc");

            // Create an aware Python datetime object in UTC
            return pyDateTimeType.Invoke(
                dt.Year.ToPython(),
                dt.Month.ToPython(),
                dt.Day.ToPython(),
                dt.Hour.ToPython(),
                dt.Minute.ToPython(),
                dt.Second.ToPython(),
                (dt.Millisecond * 1000).ToPython(), // Python uses microseconds
                pyUtc
            );
        }
    }
}