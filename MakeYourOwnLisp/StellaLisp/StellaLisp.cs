namespace StellaLisp;

public static class StellaLisp
{
    // Why are we making it possible to use a reader? 
    // so we can test it much easier.
    public static string Read(TextReader? reader = null)
    {
        // If the reader is null we use the default standard input
        reader ??= Console.In;

        // We use the Console.Readline if no reader was provided
        // Otherwise we use the reader.
        string? input = reader.ReadLine();

        // Should the input be null we simply return an empty string.
        return input ?? "";
    }

    public static string Eval(string input)
    {
        return input;
    }

    public static string Print(string input)
    {
        return input;
    }

    public static string Rep()
    {
        return Print(Eval(Read()));
    }

    public static void REPL()
    {
        string? input;
        while ((input = Console.ReadLine()) != null)
        {
            Console.WriteLine(Print(Eval(input)));
        }
    }
}
