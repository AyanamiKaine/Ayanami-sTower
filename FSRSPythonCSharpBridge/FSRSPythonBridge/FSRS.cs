using Python.Runtime;
namespace FSRSPythonBridge;

public class FSRS
{
    public FSRS()
    {
        // Set the path to your Python DLL
        Runtime.PythonDLL = @"/lib64/libpython3.so"; // Replace with your actual path

        // Initialize the Python engine
        PythonEngine.Initialize();

        Py.Import("FSRS");
    }
}
