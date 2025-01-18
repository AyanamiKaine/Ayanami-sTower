using System.ComponentModel.Design;
using System.Text.RegularExpressions;

namespace jlox;

public class ParseError(string message) : Exception(message);

public class Parser(List<Token> tokens)
{
    private readonly List<Token> Tokens = tokens;
    private int Current { get; set; } = 0;

    public List<Statement> Parse()
    {
        List<Statement> statements = [];
        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }

        return statements;
    }

    /// <summary>
    /// Used when we want to simply parse one expression
    /// this is in comparision to Parse() that works with 
    /// statements
    /// </summary>
    /// <returns></returns>
    public Expr? ParseAndReturnExpression()
    {
        return Expression();
    }

    private Statement? Declaration()
    {
        try
        {
            if (Match(TokenType.VAR))
                return VarDeclaration();
            return Statement();
        }
        catch (ParseError)
        {
            Synchronize();
            return null;
        }
    }

    private Statement VarDeclaration()
    {
        Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

        Expr initializer = null;
        if (Match(TokenType.EQUAL))
            initializer = Expression();

        Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
        return new Statement.Var(name, initializer);
    }

    private Statement Statement()
    {
        if (Match(TokenType.IF))
            return IfStatement();
        if (Match(TokenType.PRINT))
            return PrintStatement();
        if (Match(TokenType.LEFT_BRACE))
            return new Statement.Block(Block());
        return ExpressionStatement();
    }

    private Statement IfStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

        var thenBranch = Statement();
        Statement? elseBranch = null;
        if (Match(TokenType.ELSE))
            elseBranch = Statement();

        return new Statement.If(condition, thenBranch, elseBranch);
    }

    private List<Statement> Block()
    {
        var statements = new List<Statement>();

        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }

    private Statement PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after value.");
        return new Statement.Print(value);
    }

    private Statement ExpressionStatement()
    {
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
        return new Statement.Expression(value);
    }
    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Equality();

        if (Match(TokenType.EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable variable)
            {
                Token name = variable.name;
                return new Expr.Assign(name, value);
            }
            Error(equals, "Invalid Assignment Target.");
        }

        return expr;
    }

    private Expr Equality()
    {
        Expr expr = Comparison();


        while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
        {
            Token oper = Previous();
            Expr right = Comparison();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd())
            return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
            Current++;
        return Previous();
    }

    private Token Previous()
    {
        return Tokens[Current - 1];
    }

    private bool IsAtEnd()
    {
        return Peek().Type == TokenType.EOF;
    }

    private Token Peek()
    {
        return Tokens[Current];
    }

    private Expr Comparison()
    {
        Expr expr = Term();

        while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
        {
            Token oper = Previous();
            Expr right = Term();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Term()
    {
        Expr expr = Factor();

        while (Match(TokenType.MINUS, TokenType.PLUS))
        {
            Token oper = Previous();
            Expr right = Factor();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(TokenType.SLASH, TokenType.STAR))
        {
            Token oper = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Unary()
    {
        if (Match(TokenType.BANG, TokenType.MINUS))
        {
            Token oper = Previous();
            Expr right = Unary();
            return new Expr.Unary(oper, right);
        }

        return Primary();
    }

    private Expr Primary()
    {
        if (Match(TokenType.FALSE))
            return new Expr.Literal(false);
        if (Match(TokenType.TRUE))
            return new Expr.Literal(true);
        if (Match(TokenType.NIL))
            return new Expr.Literal(null);

        if (Match(TokenType.NUMBER, TokenType.STRING))
            return new Expr.Literal(Previous().Literal);

        if (Match(TokenType.IDENTIFIER))
            return new Expr.Variable(Previous());

        if (Match(TokenType.LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek(), "Expected Expression.");
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();

        throw Error(Peek(), message);
    }

    private ParseError Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseError(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Type == TokenType.SEMICOLON)
                return;

            switch (Peek().Type)
            {
                case TokenType.CLASS:
                case TokenType.FUN:
                case TokenType.VAR:
                case TokenType.FOR:
                case TokenType.IF:
                case TokenType.WHILE:
                case TokenType.PRINT:
                case TokenType.RETURN:
                    return;
            }

            Advance();
        }
    }
}