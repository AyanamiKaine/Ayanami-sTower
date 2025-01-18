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

        Expr? expr = parser.ParseAndReturnExpression();
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

        Expr? expr = parser.ParseAndReturnExpression();

        // If the expr is null the parse had an error
        Assert.NotNull(expr);

        var interpreter = new Interpreter();

        var expectedResult = true;
        var actualResult = interpreter.Interpret(expr);
        Assert.NotNull(actualResult);

        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void SimpleAssignment()
    {
        var expression = """var name = "Ayanami"; """;
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        var statements = parser.Parse();


        // If statements is empty we couldnt parse it correctly
        Assert.NotEmpty(statements);
        foreach (var statement in statements)
        {
            Assert.NotNull(statement);
        }

        var interpreter = new Interpreter();
        interpreter.Interpret(statements);
    }
}