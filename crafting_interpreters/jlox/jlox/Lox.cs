namespace jlox;

public class Lox
{
    static bool _hadError = false;
    public static void RunFile(string filePath)
    {
        if (!Path.Exists(filePath))
            throw new Exception($"File Path was not found: {filePath}");

        var scriptToBeScanned = File.ReadAllText(filePath);
    }

    public static void RunPrompt()
    {
        var input = Console.ReadLine();

        bool canceled = false;
        while (!canceled)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is not null)
            {
                Run(line);
                _hadError = false;
            }
            else
            {
                canceled = true;
            }
        }
    }

    public static void Run(string source)
    {
        var Scanner = new Scanner(source);
        List<Token> tokens = Scanner.ScanTokens();

        foreach (var token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    private static void Report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
    }
}
