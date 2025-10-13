using System.Runtime.InteropServices.JavaScript;
using StellaLang;

namespace StellaLang.REPL;

/// <summary>
/// This class is now designed to be called from JavaScript.
/// </summary>
public static partial class Program
{
    private static ForthInterpreter? _forth;
    private static StringHostIO? _io;

    /// <summary>
    /// The Main method is no longer needed for the interactive REPL.
    /// We keep it here so the project still has a valid entry point if needed,
    /// but it won't be called by our JavaScript.
    /// </summary>
    public static void Main(string[] args)
    {
        Console.WriteLine("C# Main method executed. The interactive REPL is managed by JavaScript.");
    }

    /// <summary>
    /// This method is exported to JavaScript. JS will call this once to set up the interpreter.
    /// It returns a welcome message.
    /// </summary>
#if BROWSER
    [JSExport]
    [System.Runtime.Versioning.SupportedOSPlatform("browser")]
#endif
    public static string InitializeInterpreter()
    {
        var vm = new VM();
        _io = new StringHostIO();
        _forth = new ForthInterpreter(vm, _io);

        return "Interpreter Initialized. Ready for input.";
    }

    /// <summary>
    /// This method is exported to JavaScript. It takes a line of input from the user,
    /// processes it, and returns the result as a string with any output produced.
    /// </summary>
#if BROWSER
    [JSExport]
    [System.Runtime.Versioning.SupportedOSPlatform("browser")]
#endif
    public static string ProcessInput(string input)
    {
        if (_forth == null || _io == null)
        {
            return "ERROR: Interpreter not initialized. Call InitializeInterpreter() first.";
        }

        try
        {
            // Clear previous output
            _io.Clear();

            // Interpret the input
            _forth.Interpret(input);

            // Get the output and error
            var output = _io.GetOutput();
            var error = _io.GetError();

            // Combine output and errors
            var result = output;
            if (!string.IsNullOrEmpty(error))
            {
                result += error;
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }
}