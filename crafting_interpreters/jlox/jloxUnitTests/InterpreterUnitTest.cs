using jlox;
namespace jloxUnitTests;

public class InterpreterUnitTest
{
    [Fact]
    public void SimpleMathExpression()
    {
        var expression = """( 1 + 2 ) * 3""";
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        Expr? expr = parser.Parse();
        Assert.NotNull(expr);


        var interpreter = new Interpreter();

        var expectedResult = 9d;
        var actualResult = interpreter.Interpret(expr);
        Assert.NotNull(actualResult);

        Assert.Equal(expectedResult, actualResult);
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

        var interpreter = new Interpreter();

        var expectedResult = true;
        var actualResult = interpreter.Interpret(expr);
        Assert.NotNull(actualResult);

        Assert.Equal(expectedResult, actualResult);
    }
}