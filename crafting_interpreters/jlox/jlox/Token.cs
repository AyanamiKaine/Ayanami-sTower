namespace jlox;

public enum TokenType
{
    // Single-character tokens. 
    LEFT_PAREN,
    RIGHT_PAREN,
    LEFT_BRACE,
    RIGHT_BRACE,
    COMMA,
    DOT,
    MINUS,
    PLUS,
    SEMICOLON,
    SLASH,
    STAR,

    // One or two character tokens. 
    BANG,
    BANG_EQUAL,
    EQUAL,
    EQUAL_EQUAL,
    GREATER,
    GREATER_EQUAL,
    LESS,
    LESS_EQUAL,

    // Literals. 
    IDENTIFIER,
    STRING,
    NUMBER,

    // Keywords. 
    AND,
    CLASS,
    ELSE,
    FALSE,
    FUN,
    FOR,
    IF,
    NIL,
    OR,
    PRINT,
    RETURN,
    SUPER,
    THIS,
    TRUE,
    VAR,
    WHILE,
    EOF
}

public class Token(TokenType type, string lexeme, object? literal, int line)
{
    public readonly TokenType Type = type;
    public readonly string Lexeme = lexeme;
    public readonly object? Literal = literal;
    public readonly int Line = line;

    public override string ToString()
    {
        return $"{Type} {Lexeme} {Literal}";
    }

    public int CompareTo(Token? other)
    {
        if (other is null) return 1;

        // Example comparison logic: Compare by Line, then Type, then Lexeme
        int lineComparison = Line.CompareTo(other.Line);
        if (lineComparison != 0) return lineComparison;

        int typeComparison = Type.CompareTo(other.Type);
        if (typeComparison != 0) return typeComparison;

        return Lexeme.CompareTo(other.Lexeme);
        // You might need to handle Literal comparison differently, depending on its possible types
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || GetType() != obj.GetType())
            return false;

        Token other = (Token)obj;
        return Type == other.Type &&
               Lexeme == other.Lexeme &&
               Line == other.Line &&
               EqualityComparer<object?>.Default.Equals(Literal, other.Literal);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Lexeme, Line, Literal);
    }

    public static bool operator ==(Token left, Token right)
    {
        return EqualityComparer<Token>.Default.Equals(left, right);
    }

    public static bool operator !=(Token left, Token right)
    {
        return !(left == right);
    }
}