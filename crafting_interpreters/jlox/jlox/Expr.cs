namespace jlox;

public abstract class Expr
{
    public interface IVisitor<R>
    {
        R VisitAssignExpr(Assign expr);
        R VisitBinaryExpr(Binary expr);
        R VisitCallExpr(Call expr);
        R VisitGetExpr(Get expr);
        R VisitGroupingExpr(Grouping expr);
        R VisitLiteralExpr(Literal expr);
        R VisitLogicalExpr(Logical expr);
        R VisitSetExpr(Set expr);
        R VisitSuperExpr(Super expr);
        R VisitThisExpr(This expr);
        R VisitUnaryExpr(Unary expr);
        R VisitVariableExpr(Variable expr);
    }

    public class Assign(Token name, Expr value) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitAssignExpr(this);
        }

        public readonly Token name = name;
        public readonly Expr value = value;
    }

    public class Binary(Expr left, Token op, Expr right) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitBinaryExpr(this);
        }

        public readonly Expr left = left;
        public readonly Token op = op;
        public readonly Expr right = right;
    }

    public class Call(Expr callee, Token paren, List<Expr> arguments) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitCallExpr(this);
        }

        public readonly Expr callee = callee;
        public readonly Token paren = paren;
        public readonly List<Expr> arguments = arguments;
    }

    public class Get(Expr obj, Token name) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitGetExpr(this);
        }

        public readonly Expr obj = obj;
        public readonly Token name = name;
    }

    public class Grouping(Expr expression) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitGroupingExpr(this);
        }

        public readonly Expr expression = expression;
    }

    public class Literal(object? value) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitLiteralExpr(this);
        }

        public readonly object? value = value;
    }

    public class Logical(Expr left, Token op, Expr right) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitLogicalExpr(this);
        }

        public readonly Expr left = left;
        public readonly Token op = op;
        public readonly Expr right = right;
    }

    public class Set(Expr obj, Token name, Expr value) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitSetExpr(this);
        }

        public readonly Expr obj = obj;
        public readonly Token name = name;
        public readonly Expr value = value;
    }

    public class Super(Token keyword, Token method) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitSuperExpr(this);
        }

        public readonly Token keyword = keyword;
        public readonly Token method = method;
    }

    public class This(Token keyword) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitThisExpr(this);
        }

        public readonly Token keyword = keyword;
    }

    public class Unary(Token op, Expr right) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitUnaryExpr(this);
        }

        public readonly Token op = op;
        public readonly Expr right = right;
    }

    public class Variable(Token name) : Expr
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitVariableExpr(this);
        }

        public readonly Token name = name;
    }

    public abstract R Accept<R>(IVisitor<R> visitor);
}