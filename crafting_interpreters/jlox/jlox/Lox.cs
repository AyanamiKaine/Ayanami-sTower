namespace jlox;

public class Lox
{
    private readonly Interpreter Interpreter = new();
    private bool _hadError = false;
    private bool _hadRunTimeError = false;
    public void RunFile(string filePath)
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

    public void RunPrompt()
    {
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

    public void Run(string source)
    {
        var Scanner = new Scanner(source);
        List<Token> tokens = Scanner.ScanTokens();
        var parser = new Parser(tokens);
        List<Statement> statements = parser.Parse();

        Resolver resolver = new(Interpreter);
        resolver.Resolve(statements);

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
