namespace jlox;

class Resolver(Interpreter interpreter) : Expr.IVisitor<object?>, Statement.IVisitor<object?>
{
    private readonly Interpreter _interpreter = interpreter;
    private readonly Stack<Dictionary<string, bool>> _scopes = new();
    public object? VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.value);
        ResolveLocal(expr, expr.name);
        return null;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return null;
    }

    public object? VisitBlockStmt(Statement.Block stmt)
    {
        BeginScope();
        Resolve(stmt.statements);
        EndScope();
        return null;
    }

    public void Resolve(List<Statement> statements)
    {
        foreach (var statement in statements)
        {
            Resolve(statement);
        }
    }

    public void Resolve(Statement statement)
    {
        statement.Accept(this);
    }

    public void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    public void BeginScope()
    {
        _scopes.Push([]);
    }

    public void EndScope()
    {
        _scopes.Pop();
    }


    public object? VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.callee);

        foreach (var argument in expr.arguments)
        {
            Resolve(argument);
        }

        return null;
    }

    public object? VisitClassStmt(Statement.Class stmt)
    {
        Declare(stmt.name);
        Define(stmt.name);
        return null;
    }

    public object? VisitExpressionStmt(Statement.Expression stmt)
    {
        Resolve(stmt.expression);
        return null;
    }

    public object? VisitFunctionStmt(Statement.Function stmt)
    {
        Declare(stmt.name);
        Define(stmt.name);

        ResolveFunction(stmt);
        return null;
    }

    private void ResolveFunction(Statement.Function function)
    {
        BeginScope();
        foreach (var param in function.params_)
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.body);
        EndScope();
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.obj);
        return null;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.expression);
        return null;
    }

    public object? VisitIfStmt(Statement.If stmt)
    {
        Resolve(stmt.condition);
        Resolve(stmt.thenBranch);
        if (stmt.elseBranch is not null)
            Resolve(stmt.elseBranch);

        return null;
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return null;
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return null;
    }

    public object? VisitPrintStmt(Statement.Print stmt)
    {
        Resolve(stmt.expression);
        return null;
    }

    public object? VisitReturnStmt(Statement.Return stmt)
    {
        if (stmt.value is not null)
            Resolve(stmt.value);

        return null;
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        throw new NotImplementedException();
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.right);
        return null;
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        if (!(_scopes.Count == 0) && _scopes.Peek().ContainsKey(expr.name.Lexeme) && _scopes.Peek()[expr.name.Lexeme] == false)
        {
            Lox.Error(expr.name, "Cant read local variable in its own initalizer.");
        }

        ResolveLocal(expr, expr.name);
        return null;
    }

    public object? VisitVarStmt(Statement.Var stmt)
    {
        Declare(stmt.name);
        if (stmt.initializer is not null)
            Resolve(stmt.initializer);

        Define(stmt.name);
        return null;
    }

    public void Define(Token name)
    {
        if (_scopes.Count == 0)
            return;

        if (_scopes.Peek().ContainsKey(name.Lexeme))
            _scopes.Peek()[name.Lexeme] = true;
        else
            _scopes.Peek().Add(name.Lexeme, true);
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        Dictionary<string, bool>[] scopesArray = _scopes.ToArray();
        for (int i = 0; i < scopesArray.Length; i++)
        {
            if (scopesArray[i].ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, i);
                return;
            }
        }
    }

    public void Declare(Token name)
    {
        if (_scopes.Count == 0)
            return;

        Dictionary<string, bool> scope = _scopes.Peek();

        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(name, "Already variable with this name in this scope.");
        }

        scope.Add(name.Lexeme, false);
    }

    public object? VisitWhileStmt(Statement.While stmt)
    {
        Resolve(stmt.condition);
        Resolve(stmt.body);
        return null;
    }
}