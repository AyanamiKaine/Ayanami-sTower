using System.Globalization;
using Python.Runtime;
namespace FSRSPythonBridge;



public enum CardState
{
    Learning = 1, // (==1) new card being studied for the first time
    Review = 2, // (==2) card that has "graduated" from the Learning state
    Relearning = 3, // (==3) card that has "lapsed" from the Review state
}

public class Card(dynamic card)
{
    /// <summary>
    /// Represents the underlying python card object.
    /// </summary>
    private readonly dynamic _card = card;
    public CardState State
    {
        get
        {
            using (Py.GIL())
            {
                int state = _card.state.As<int>();
                return (CardState)state;
            }
        }
    }
    /// <summary>
    /// Returns the underlying pyobject HANDLE WITH CARE!.
    /// SHOULD ONLY BE USED IN A PYTHON CONTEXT
    /// </summary>
    public dynamic PyObject
    {
        get
        {
            return _card;
        }
    }
    public long ID
    {
        get
        {
            using (Py.GIL())
            {
                long id = _card.card_id.As<long>();
                return id;
            }
        }
    }
    public DateTime Due
    {
        get
        {
            using (Py.GIL())
            {
                string format = "yyyy-MM-dd HH:mm:ss.ffffffzzz"; // Define the exact format
                string due = _card.due.ToString();
                return DateTime.ParseExact(due, format, CultureInfo.InvariantCulture);
            }
        }
    }

    public DateTime? LastReview
    {
        get
        {
            using (Py.GIL())
            {
                if (_card.last_review == null)
                {
                    return null;
                }
                string format = "yyyy-MM-dd HH:mm:ss.ffffffzzz"; // Define the exact format
                string lastReview = _card.last_review.ToString();

                return DateTime.ParseExact(lastReview, format, CultureInfo.InvariantCulture);
            }
        }
    }

    public float? Stability
    {
        get
        {
            using (Py.GIL())
            {
                if (_card.stability == null)
                {
                    return null;
                }

                return _card.stability.As<float>();
            }
        }
    }

    public float? Difficulty
    {
        get
        {
            using (Py.GIL())
            {
                if (_card.difficulty == null)
                {
                    return null;
                }

                return _card.difficulty.As<float>();
            }
        }
    }
}