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

        Expr? expr = parser.ParseAndReturnExpression();

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

        Expr? expr = parser.ParseAndReturnExpression();

        // If the expr is null the parse had an error
        Assert.NotNull(expr);
    }

    [Fact]
    public void PrintStatementTest()
    {
        var expression = """print("Hello, World!");""";
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        var statements = parser.Parse();

        // If statements is empty we couldnt parse it correctly
        Assert.NotNull(statements);

        // We expect that we have only one statement and
        // that it is of type print
        Assert.IsType<Statement.Print>(statements[0]);
    }

    [Fact]
    public void AssignmentStatementTest()
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
    }

    [Fact]
    public void BlockStatementTest()
    {
        var expression =
        """
        var global = "outside";
        {
            var local = "inside";

            print global + local;
        }
        """;
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
    }
}