using jlox;
namespace jloxUnitTests;

public class ParserUnitTest
{
    [Fact]
    public void SimpleMathExpressionTest()
    {
        var expression = """( 1 + 2 ) * 3""";
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        Expr? expr = parser.Parse();

        // If the expr is null the parse had an error
        Assert.NotNull(expr);
    }

    [Fact]
    public void SimpleBooleanExpressionTest()
    {
        var expression = """(2 == 2) == true""";
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        Expr? expr = parser.Parse();

        // If the expr is null the parse had an error
        Assert.NotNull(expr);
    }
}