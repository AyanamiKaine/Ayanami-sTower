using System.Text.RegularExpressions;

namespace StellaLisp;

public class Token
{

}


public class Reader
{
    /// <summary>
    /// Using regex seems such a bad idea, why not a recursive decenting parser?
    /// Its not that hard to implement.
    /// </summary>
    private string tokenPattern = """[\s,]*(~@|[\[\]{}()'`~^@]|"(?:\\.|[^\\"])*"?|;.*|[^\s\[\]{}('"`,;)]*)""";

    /// <summary>
    /// returns the current token and increments the current position of the reader.
    /// </summary>
    /// <returns></returns>
    public Token Next()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the next token but does not increment the current position of the reader.
    /// </summary>
    /// <returns></returns>
    public Token Peak()
    {
        throw new NotImplementedException();
    }

    public MatchCollection ReadString(string input)
    {
        return Regex.Matches(input, tokenPattern);
    }

    public List<Token> Tokenize()
    {
        throw new NotImplementedException();
    }

    public dynamic ReadForm()
    {
        throw new NotImplementedException();
    }
}