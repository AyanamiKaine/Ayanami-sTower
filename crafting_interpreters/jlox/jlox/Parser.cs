

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
            if (Match(TokenType.CLASS))
                return ClassDeclaration();
            if (Match(TokenType.FUN))
                return Function("function");
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

    private Statement.Class? ClassDeclaration()
    {
        var name = Consume(TokenType.IDENTIFIER, "Expect class name.");

        Expr.Variable? superclass = null;
        if (Match(TokenType.LESS))
        {
            Consume(TokenType.IDENTIFIER, "Expect superclass name.");
            superclass = new Expr.Variable(Previous());
        }
        Consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");

        var methods = new List<Statement.Function>();

        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
        {
            methods.Add(Function("method"));
        }

        Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body");

        return new Statement.Class(name, superclass, methods);
    }

    private Statement.Function Function(string kind)
    {
        Token name = Consume(TokenType.IDENTIFIER, $"Expect {kind} name.");

        Consume(TokenType.LEFT_PAREN, $"Expect '(' after {kind} name");

        var parameters = new List<Token>();

        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                if (parameters.Count >= 255)
                    Error(Peek(), "Cant have more than 255 parameters.");

                parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
            } while (Match(TokenType.COMMA));
        }
        Consume(TokenType.RIGHT_PAREN, "Expect ')' affter parameters.");

        Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {kind} body");
        var body = Block();
        return new Statement.Function(name, parameters, body);
    }

    private Statement.Var VarDeclaration()
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
        if (Match(TokenType.FOR))
            return ForStatement();
        if (Match(TokenType.IF))
            return IfStatement();
        if (Match(TokenType.PRINT))
            return PrintStatement();
        if (Match(TokenType.RETURN))
            return ReturnStatement();
        if (Match(TokenType.WHILE))
            return WhileStatement();
        if (Match(TokenType.LEFT_BRACE))
            return new Statement.Block(Block());
        return ExpressionStatement();
    }

    private Statement.Return ReturnStatement()
    {
        var keyword = Previous();
        Expr? value = null;
        if (!Check(TokenType.SEMICOLON))
            value = Expression();

        Consume(TokenType.SEMICOLON, "Expect ';' after return value ");
        return new Statement.Return(keyword, value);
    }

    private Statement ForStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");
        Statement? initializer;
        if (Match(TokenType.SEMICOLON))
            initializer = null;
        else if (Match(TokenType.VAR))
            initializer = VarDeclaration();
        else
            return ExpressionStatement();

        Expr condition = null;
        if (!Check(TokenType.SEMICOLON))
            condition = Expression();

        Consume(TokenType.SEMICOLON, "Expect ';' after loop condition");

        Expr increment = null;
        if (!Check(TokenType.RIGHT_PAREN))
            increment = Expression();

        Consume(TokenType.RIGHT_PAREN, "Expect ')' after a clauses");

        var body = Statement();

        if (increment is not null)
        {
            body = new Statement.Block([
                body,
                new Statement.Expression(increment)
            ]);
        }

        condition ??= new Expr.Literal(true);
        body = new Statement.While(condition, body);

        if (initializer is not null)
        {
            body = new Statement.Block([
                initializer,
                body
            ]);
        }

        return body;
    }

    private Statement.While WhileStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after while.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition");
        var body = Statement();

        return new Statement.While(condition, body);
    }

    private Statement.If IfStatement()
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

    private Expr Or()
    {
        Expr expr = And();

        while (Match(TokenType.OR))
        {
            Token op = Previous();
            Expr right = And();
            expr = new Expr.Logical(expr, op, right);
        }

        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();

        while (Match(TokenType.AND))
        {
            Token op = Previous();
            Expr right = Equality();
            expr = new Expr.Logical(expr, op, right);
        }

        return expr;
    }

    private Statement.Print PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after value.");
        return new Statement.Print(value);
    }

    private Statement.Expression ExpressionStatement()
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
        Expr expr = Or();

        if (Match(TokenType.EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable variable)
            {
                Token name = variable.name;
                return new Expr.Assign(name, value);
            }
            else if (expr is Expr.Get get)
            {
                return new Expr.Set(get.obj, get.name, value);
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

        return Call();
    }

    private Expr Call()
    {
        var expr = Primary();

        while (true)
        {
            if (Match(TokenType.LEFT_PAREN))
                expr = FinishCall(expr);
            else if (Match(TokenType.DOT))
            {
                var name = Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else
                break;
        }

        return expr;
    }

    private Expr.Call FinishCall(Expr callee)
    {
        var arguments = new List<Expr>();
        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                if (arguments.Count >= 255)
                    Error(Peek(), "Cant have more than 255 arguments.");
                arguments.Add(Expression());
            } while (Match(TokenType.COMMA));
        }

        var paren = Consume(TokenType.RIGHT_PAREN, "Expect, ')' after arguments");

        return new Expr.Call(callee, paren, arguments);
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

        if (Match(TokenType.SUPER))
        {
            var keyword = Previous();
            Consume(TokenType.DOT, "Expect '.' after 'super' .");
            var method = Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        }

        if (Match(TokenType.THIS))
            return new Expr.This(Previous());

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