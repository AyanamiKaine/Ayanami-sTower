namespace clox;
public static class Compiler
{
    public static void Compile(string source)
    {
        var Scanner = new Scanner(source);
        List<Token> tokens = Scanner.ScanTokens();
    }
}