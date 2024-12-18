using System.Runtime.InteropServices;
using Python.Runtime;
namespace FSRSPythonBridge;

/// <summary>
/// Used to determine the rating of a card.
/// </summary>
public enum Rating
{
    /// <summary>
    /// Rating.Again (==1) forgot the card
    /// </summary>
    Again = 1,
    /// <summary>
    /// Rating.Hard (==2) remembered the card with serious difficulty
    /// </summary>
    Hard = 2,
    /// <summary>
    /// Rating.Good (==3) remembered the card after a hesitation
    /// </summary>
    Good = 3,
    /// <summary>
    /// Rating.Easy (==4) remembered the card easily
    /// </summary>
    Easy = 4,
}

/// <summary>
/// Provides access to the Python FSRS Module 4.1.0
/// For more information on how to use it see: "https://github.com/open-spaced-repetition/py-fsrs"
/// </summary>
public static class FSRS
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
            SettingUpModulePath();
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

    /// <summary>
    /// Here we are adding the path to the fsrs module to the sys.path.
    /// so we can import it.
    /// </summary>
    private static void SettingUpModulePath()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string pythonPath = Path.Combine(appDir, "python");

        string newPythonPath = $"{pythonPath}";

        using (Py.GIL())
        {
            dynamic sys = Py.Import("sys");
            sys.path.append(newPythonPath);
        }
    }

    private static void SettingUpRuntimePythonDLL()
    {
        // For Linux we use the system python library.
        // as we expect the user to have python installed.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (File.Exists(@"/lib64/libpython3.so"))
                Runtime.PythonDLL = @"/lib64/libpython3.so";
            else throw new DllNotFoundException("Could not find libpython3.so at /lib64/libpython3.so");
        }
        // For Windows we ship the python313.dll with the application. 
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string pythonPath = Path.Combine(appDir, "python", "windows");
            Runtime.PythonDLL = Path.Combine(pythonPath, "python313.dll");
        }
        else
        {
            throw new PlatformNotSupportedException("OS not supported");
        }
    }

    /// <summary>
    /// Creates a new spaced repetition card.
    /// </summary>
    /// <returns></returns>
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
