using Python.Runtime;
namespace FSRSPythonBridge;

public class FSRS
{
    public FSRS()
    {
        try
        {
            // Set the path to your Python DLL
            Runtime.PythonDLL = @"/lib64/libpython3.so"; // Replace with your actual path

            // Initialize the Python engine
            PythonEngine.Initialize();

            using (Py.GIL())
            {
                var fsrs = PyModule.Import("fsrs");
            }

        }
        catch (System.Exception e)
        {
            Console.WriteLine("Error initializing Python engine");
            Console.WriteLine(e.Message);
            //throw;
        }
    }
}
