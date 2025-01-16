using jlox;

namespace jloxUnitTests;

public class ASTPrinterUnitTest
{
    [Fact]
    public void SimpleExpressionTest()
    {
        var ASTPrinter = new ASTPrinter();
        var expression = new Expr.Binary(
            new Expr.Unary(
                new Token(TokenType.MINUS, "-", null, 1),
                 new Expr.Literal(123)),
            new Token(TokenType.STAR, "*", null, 1),
            new Expr.Grouping(
                new Expr.Literal(45.67)));

        var expectedExpressionString = "(* (- 123) (group 45.67))";
        var actualExpressionString = ASTPrinter.Print(expression);

        Assert.Equal(expectedExpressionString, actualExpressionString);
    }

}