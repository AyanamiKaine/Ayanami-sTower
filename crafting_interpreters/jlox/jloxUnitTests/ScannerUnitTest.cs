using jlox;
namespace jloxUnitTests;

public class ScannerUnitTest
{
    [Fact]
    public void SimpleExpression()
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
}
