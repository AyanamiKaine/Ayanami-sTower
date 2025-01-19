namespace jlox;
public class RuntimeError(Token token, string message) : Exception(message)
{
    public readonly Token Token = token;
};



public class Interpreter : Expr.IVisitor<object>, Statement.IVisitor<object?>
{

    /// <summary>
    /// Defined a native clock function
    /// </summary>
    private class Clock : LoxCallable
    {
        public int Arity()
        {
            return 0;
        }

        public dynamic Call(Interpreter interpreter, List<dynamic> arguments)
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public override string ToString()
        {
            return "<native fn>";
        }
    }


    private LoxEnvironment _globals;

    private LoxEnvironment _env;

    public Interpreter()
    {
        _globals = new();
        _env = _globals;

        _globals.Define("clock", new Clock());
    }

    public dynamic? Interpret(Expr expr)
    {
        try
        {
            dynamic value = Evaluate(expr);
            return value;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Interpret(List<Statement> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private string Stringify(dynamic obj)
    {
        if (obj is null)
            return "nil";

        /* This code was mostly done for java, i dont think we need it for C#
        if (obj is double value)
        {
            var text = value.ToString();
            if (text.EndsWith(".0"))
                text = text.SubstringByIndex(0, text.Length - 2);

            return text;
        }
        */
        return obj.ToString();
    }

    private void Execute(Statement statement)
    {
        statement.Accept(this);
    }

    public dynamic Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    public dynamic VisitAssignExpr(Expr.Assign expr)
    {
        var value = Evaluate(expr.value);

        _env.Assign(expr.name, value);
        return value;
    }

    public dynamic VisitBinaryExpr(Expr.Binary expr)
    {
        dynamic left = Evaluate(expr.left);
        dynamic right = Evaluate(expr.right);

        return expr.op.Type switch
        {
            TokenType.GREATER =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left > (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.GREATER_EQUAL =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left >= (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.LESS =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left < (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.LESS_EQUAL =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left <= (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.MINUS =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left - (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.SLASH =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left / (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.STAR =>
            CheckNumberOperands(expr.op, left, right)
            ? (double)left * (double)right
            : throw new RuntimeError(expr.op, "Operands must be numbers."),

            TokenType.PLUS =>
            (left is double v1 && right is double v3) ? v1 + v3
            : (left is string v && right is string v2) ? v + v2
            : throw new RuntimeError(expr.op, "Operands must be two numbers or two strings."),

            TokenType.BANG_EQUAL => !IsEqual(left, right),
            TokenType.EQUAL_EQUAL => !IsEqual(left, right),

            _ => throw new Exception("CANT"),
        };
    }

    private bool IsEqual(dynamic left, dynamic right)
    {
        if (left is null && right is null)
            return true;
        if (left is null)
            return false;

        return left == right;
    }

    public dynamic VisitCallExpr(Expr.Call expr)
    {
        dynamic callee = Evaluate(expr.callee);

        var arguments = new List<dynamic>();
        foreach (var argument in arguments)
        {
            arguments.Add(Evaluate(argument));
        }


        if (callee is not LoxCallable)
            throw new RuntimeError(expr.paren, "Can only call functions and classes.");

        LoxCallable function = (LoxCallable)callee;

        if (arguments.Count != function.Arity())
            throw new RuntimeError(expr.paren, $"Expected {function.Arity()} arguments but got {arguments.Count} .");



        return function.Call(this, arguments);
    }

    public dynamic VisitGetExpr(Expr.Get expr)
    {
        throw new NotImplementedException();
    }

    public dynamic VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.expression);
    }

    public dynamic VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.value!;
    }

    public dynamic VisitLogicalExpr(Expr.Logical expr)
    {
        dynamic left = Evaluate(expr.left);

        if (expr.op.Type == TokenType.OR)
        {
            if (IsTruthy(left))
                return left;
        }
        else
        {
            if (!IsTruthy(left))
                return left;
        }

        return Evaluate(expr.right);
    }

    public dynamic VisitSetExpr(Expr.Set expr)
    {
        throw new NotImplementedException();
    }

    public dynamic VisitSuperExpr(Expr.Super expr)
    {
        throw new NotImplementedException();
    }

    public dynamic VisitThisExpr(Expr.This expr)
    {
        throw new NotImplementedException();
    }

    public dynamic VisitUnaryExpr(Expr.Unary expr)
    {
        dynamic right = Evaluate(expr.right);

        return expr.op.Type switch
        {
            TokenType.BANG => !IsTruthy(right),
            TokenType.MINUS =>
            CheckNumberOperand(expr.op, right)
            ? -(double)right
            : throw new RuntimeError(expr.op, "Operand must be a number"),
            _ => throw new Exception("CANT"),
        };
    }

    private bool CheckNumberOperands(Token op, dynamic left, dynamic right)
    {
        if (right is double && left is double)
            return true;
        throw new RuntimeError(op, "Operands must be a numbers.");
    }

    private bool CheckNumberOperand(Token op, dynamic right)
    {
        if (right is double)
            return true;
        throw new RuntimeError(op, "Operand must be a number");
    }

    public dynamic VisitVariableExpr(Expr.Variable expr)
    {
        return _env.Get(expr.name);
    }

    private static bool IsTruthy(object obj)
    {
        if (obj is null) return false;
        if (obj is bool boolean) return boolean;
        return true;
    }

    object? Statement.IVisitor<object?>.VisitBlockStmt(Statement.Block stmt)
    {
        ExecuteBlock(stmt.statements, new LoxEnvironment(_env));
        return null;
    }

    private void ExecuteBlock(List<Statement> statements, LoxEnvironment loxEnvironment)
    {
        var previous = _env;

        try
        {
            // Now, that field represents the current environment
            _env = loxEnvironment;
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _env = previous;
        }
    }

    object? Statement.IVisitor<object?>.VisitClassStmt(Statement.Class stmt)
    {
        throw new NotImplementedException();
    }

    object? Statement.IVisitor<object?>.VisitExpressionStmt(Statement.Expression stmt)
    {
        Evaluate(stmt.expression);
        return null;
    }

    object? Statement.IVisitor<object?>.VisitFunctionStmt(Statement.Function stmt)
    {
        throw new NotImplementedException();
    }

    object? Statement.IVisitor<object?>.VisitIfStmt(Statement.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.condition)))
            Execute(stmt.thenBranch);
        else if (stmt.elseBranch is not null)
            Execute(stmt.elseBranch);

        return null;
    }

    object? Statement.IVisitor<object?>.VisitPrintStmt(Statement.Print stmt)
    {
        dynamic value = Evaluate(stmt.expression);
        Console.WriteLine(value);
        return null;
    }

    object? Statement.IVisitor<object?>.VisitReturnStmt(Statement.Return stmt)
    {
        throw new NotImplementedException();
    }

    object? Statement.IVisitor<object?>.VisitVarStmt(Statement.Var stmt)
    {
        dynamic? value = null;
        if (stmt.initializer is not null)
            value = Evaluate(stmt.initializer);

        _env.Define(stmt.name.Lexeme, value);
        return null;
    }

    object? Statement.IVisitor<object?>.VisitWhileStmt(Statement.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.condition)))
        {
            Execute(stmt.body);
        }
        return null;
    }
}