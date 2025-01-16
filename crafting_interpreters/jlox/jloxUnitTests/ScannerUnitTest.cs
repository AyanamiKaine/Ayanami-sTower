using jlox;
namespace jloxUnitTests;

public class ScannerUnitTest
{
    [Fact]
    public void SimpleVariableAssignment()
    {
        var expression = """var name = "Ayanami"; """;
        var scanner = new Scanner(expression);

        var actualTokens = scanner.ScanTokens();

        var expectedTokens = new List<Token>()
        {
            new(TokenType.VAR, "var", null, 1),
            new(TokenType.IDENTIFIER, "name", null, 1),
            new(TokenType.EQUAL, "=", null, 1),
            new(TokenType.STRING, "\"Ayanami\"", "Ayanami", 1),
            new(TokenType.SEMICOLON, ";", null, 1),
            new(TokenType.EOF, "", null, 1)
        };

        Assert.Equal(expectedTokens, actualTokens);
    }

    [Fact]
    public void SimepleMathExpression()
    {
        var expression = """var value = 1 * 2 + 2 - 1;""";
        var scanner = new Scanner(expression);

        var actualTokens = scanner.ScanTokens();

        //Numbers are doubles by default!
        var expectedTokens = new List<Token>()
        {
            new(TokenType.VAR, "var", null, 1),
            new(TokenType.IDENTIFIER, "value", null, 1),
            new(TokenType.EQUAL, "=", null, 1),
            new(TokenType.NUMBER, "1", 1d, 1),
            new(TokenType.STAR, "*", null, 1),
            new(TokenType.NUMBER, "2", 2d, 1),
            new(TokenType.PLUS, "+", null, 1),
            new(TokenType.NUMBER, "2", 2d, 1),
            new(TokenType.MINUS, "-", null, 1),
            new(TokenType.NUMBER, "1", 1d, 1),
            new(TokenType.SEMICOLON, ";", null, 1),
            new(TokenType.EOF, "", null, 1)
        };

        Assert.Equal(expectedTokens, actualTokens);
    }
}
