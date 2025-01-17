namespace jlox;

public class Lox
{
    static private readonly Interpreter Interpreter = new();
    private static bool _hadError = false;
    private static bool _hadRunTimeError = false;
    public static void RunFile(string filePath)
    {
        if (!Path.Exists(filePath))
            throw new Exception($"File Path was not found: {filePath}");

        var scriptToBeScanned = File.ReadAllText(filePath);
        Run(scriptToBeScanned);
        if (_hadError)
            Environment.Exit(65);
        if (_hadRunTimeError)
            Environment.Exit(70);
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
        var parser = new Parser(tokens);
        List<Statement> statements = parser.Parse();

        Interpreter.Interpret(statements);
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    public static void Error(Token token, string message)
    {
        if (token.Type == TokenType.EOF)
        {
            Report(token.Line, " at end", message);
        }
        else
        {
            Report(token.Line, $"at '{token.Lexeme}'", message);
        }
    }

    private static void Report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
    }
}
