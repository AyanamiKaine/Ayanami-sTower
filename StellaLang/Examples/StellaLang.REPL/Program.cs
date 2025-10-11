using StellaLang;

namespace StellaLang.REPL;

/// <summary>
/// Simple console application that runs the StellaLang FORTH interpreter REPL.
/// </summary>
static class Program
{
    static void Main(string[] args)
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.REPL();
    }
}
