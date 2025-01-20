namespace clox;

public class Scanner(string source)
{
    private string _source = source;
    private List<Token> _tokens = [];

    private int _start = 0;
    private int _current = 0;
    private int _line = 1;

    private static readonly Dictionary<string, TokenType> _keywords = new()
    {
        { "and", TokenType.AND },
        { "class", TokenType.CLASS },
        { "return", TokenType.RETURN },
        { "else", TokenType.ELSE },
        { "false", TokenType.FALSE },
        { "for", TokenType.FOR },
        { "fun", TokenType.FUN },
        { "if", TokenType.IF },
        { "nil", TokenType.NIL },
        { "or", TokenType.OR },
        { "print", TokenType.PRINT },
        { "super", TokenType.SUPER },
        { "this", TokenType.THIS },
        { "true", TokenType.TRUE },
        { "var", TokenType.VAR },
        { "while", TokenType.WHILE },
    };
    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }
        _tokens.Add(new Token(TokenType.EOF, "", null, _line));
        return _tokens;
    }

    private void ScanToken()
    {
        var character = Advance();

        switch (character)
        {
            case '(': AddToken(TokenType.LEFT_PAREN); break;
            case ')': AddToken(TokenType.RIGHT_PAREN); break;
            case '{': AddToken(TokenType.LEFT_BRACE); break;
            case '}': AddToken(TokenType.RIGHT_BRACE); break;
            case ',': AddToken(TokenType.COMMA); break;
            case '.': AddToken(TokenType.DOT); break;
            case '-': AddToken(TokenType.MINUS); break;
            case '+': AddToken(TokenType.PLUS); break;
            case ';': AddToken(TokenType.SEMICOLON); break;
            case '*': AddToken(TokenType.STAR); break;

            case '!':
                AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                break;

            case '/':
                if (Match('/'))
                {
                    while (Peek() != '/' && !IsAtEnd())
                        Advance();
                }
                else
                {
                    AddToken(TokenType.SLASH);
                }
                break;

            case ' ':
            case '\r':
            case '\t': // Ignore whitespace. 
                break;

            case '\n':
                _line++; break;

            case '"':
                String();
                break;

            default:
                if (IsDigit(character))
                    Number();
                else if (IsAlpha(character))
                    Identifier();
                else
                    throw new Exception($"Unexpected character at line: {_line}");
                break;
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek()))
            Advance();

        var text = _source.SubstringByIndex(_start, _current - 1);

        if (_keywords.TryGetValue(text, out TokenType type))
            AddToken(type);
        else
            AddToken(TokenType.IDENTIFIER);
    }

    private bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }

    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z') ||
                c == '_';
    }

    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private void Number()
    {
        while (IsDigit(Peek()))
            Advance();

        // Look for a fractional part.
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();
            while (IsDigit(Peek()))
                Advance();
        }

        AddToken(TokenType.NUMBER, double.Parse(_source.SubstringByIndex(_start, _current - 1)));

    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
                _line++;
            Advance();
        }

        if (IsAtEnd())
        {
            throw new Exception($"Unterminated string at line: {_line}");
        }

        //We found the closing "!
        Advance();

        var value = _source.SubstringByIndex(_start + 1, _current - 2);
        AddToken(TokenType.STRING, value);
    }

    private char PeekNext()
    {
        if (_current + 1 >= _source.Length)
            return '\0';
        return _source[_current + 1];
    }

    private char Peek()
    {
        if (IsAtEnd())
            return '\0';
        return _source[_current];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd())
            return false;

        if (_source[_current] != expected)
            return false;

        _current++;
        return true;
    }

    private char Advance()
    {
        var nextChar = _source[_current];
        _current++;

        return nextChar;
    }

    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    private void AddToken(TokenType type, object? literal)
    {
        var text = _source.SubstringByIndex(_start, _current - 1);
        _tokens.Add(new Token(type, text, literal, _line));
    }

    private bool IsAtEnd()
    {
        return _current >= _source.Length;
    }
}