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
                return id;
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
                const string format = "yyyy-MM-dd HH:mm:ss.ffffffzzz"; // Define the exact format
                string due = PyObject.due.ToString();
                return DateTime.ParseExact(due, format, CultureInfo.InvariantCulture);
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
                if (PyObject.last_review == null)
                {
                    return null;
                }
                const string format = "yyyy-MM-dd HH:mm:ss.ffffffzzz"; // Define the exact format
                string lastReview = PyObject.last_review.ToString();

                return DateTime.ParseExact(lastReview, format, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    ///  Core mathematical parameter used for future scheduling.
    /// </summary>
    public float? Stability
    {
        get
        {
            using (Py.GIL())
            {
                if (PyObject.stability == null)
                {
                    return null;
                }

                return PyObject.stability.As<float>();
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
                if (PyObject.difficulty == null)
                {
                    return null;
                }

                return PyObject.difficulty.As<float>();
            }
        }
    }
}