using System.Runtime.InteropServices;
using Python.Runtime;
namespace FSRSPythonBridge;


public enum Rating
{
    Again = 1,  // Rating.Again (==1) forgot the card
    Hard = 2,   // Rating.Hard (==2) remembered the card with serious difficulty
    Good = 3,   // Rating.Good (==3) remembered the card after a hesitation
    Easy = 4,   // Rating.Easy (==4) remembered the card easily
}

public class FSRS
{
    /// <summary>
    /// All python objects should be declared as dynamic type.
    /// </summary>
    private static readonly dynamic _fsrsModule;
    private static readonly dynamic _scheduler;

    static FSRS()
    {
        try
        {
            SettingUpRuntimePythonDLL();
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();

            /*
            All calls to python should be inside a 
            using (Py.GIL()) 
            {/Your code here }block.
            */

            using (Py.GIL())
            {
                _fsrsModule = Py.Import("fsrs");
                _scheduler = _fsrsModule.Scheduler() ?? throw new Exception("Could not create scheduler object");
                if (_fsrsModule == null)
                {
                    dynamic sys = Py.Import("sys");
                    Console.WriteLine("Python sys.path:");

                    string checkedPathsForModules = string.Empty;

                    foreach (var path in sys.path)
                    {
                        checkedPathsForModules += path.ToString() + Environment.NewLine;
                    }
                    throw new Exception("Could not import fsrs module in the following paths:" + Environment.NewLine + checkedPathsForModules);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error initializing Python engine");
            Console.WriteLine(e.Message);
            throw;
        }
    }

    private static void SettingUpRuntimePythonDLL()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Runtime.PythonDLL = @"/lib64/libpython3.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string pythonPath = Path.Combine(appDir, "python", "windows");
            // Set the Python DLL path explicitly (adjust the name if needed)
            Runtime.PythonDLL = Path.Combine(pythonPath, "python313.dll");
        }
        else
            throw new PlatformNotSupportedException("OS not supported");
    }

    public static Card CreateCard()
    {
        using (Py.GIL())
        {
            return new Card(_fsrsModule.Card());
        }
    }

    /// <summary>
    /// Rates a card and returns a new updated card
    /// with it new values and new due date.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="rating"></param>
    /// <returns></returns>
    public static Card RateCard(Card card, Rating rating)
    {
        using (Py.GIL())
        {
            // review_card returns a tuple see: "https://github.com/open-spaced-repetition/py-fsrs" for more information.
            dynamic tuple = _scheduler.review_card(card.PyObject, (int)rating);
            dynamic updatedCard = tuple[0];
            dynamic reviewLog = tuple[1];

            return new(updatedCard);
        }
    }
}
