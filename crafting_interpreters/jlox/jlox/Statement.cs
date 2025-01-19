//> Appendix II stmt
namespace jlox;

using System.Collections.Generic;

public abstract class Statement
{
    public interface IVisitor<R>
    {
        public R VisitBlockStmt(Block stmt);
        public R VisitClassStmt(Class stmt);
        public R VisitExpressionStmt(Expression stmt);
        public R VisitFunctionStmt(Function stmt);
        public R VisitIfStmt(If stmt);
        public R VisitPrintStmt(Print stmt);
        public R VisitReturnStmt(Return stmt);
        public R VisitVarStmt(Var stmt);
        public R VisitWhileStmt(While stmt);
    }

    // Nested Stmt classes here...
    //> stmt-block
    public class Block(List<Statement> statements) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }

        public readonly List<Statement> statements = statements;
    }
    //< stmt-block
    //> stmt-class
    public class Class(Token name,
                 Expr.Variable superclass,
                 List<Statement.Function> methods) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitClassStmt(this);
        }

        public readonly Token name = name;
        public readonly Expr.Variable superclass = superclass;
        public readonly List<Statement.Function> methods = methods;
    }
    //< stmt-class
    //> stmt-expression
    public class Expression(Expr expression) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }

        public readonly Expr expression = expression;
    }
    //< stmt-expression
    //> stmt-function
    public class Function(Token name, List<Token> parameters, List<Statement> body) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitFunctionStmt(this);
        }

        public readonly Token name = name;
        public readonly List<Token> params_ = parameters; // Renamed field
        public readonly List<Statement> body = body;
    }
    //< stmt-function
    //> stmt-if
    public class If(Expr condition, Statement thenBranch, Statement elseBranch) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitIfStmt(this);
        }

        public readonly Expr condition = condition;
        public readonly Statement thenBranch = thenBranch;
        public readonly Statement elseBranch = elseBranch;
    }
    //< stmt-if
    //> stmt-print
    public class Print(Expr expression) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }

        public readonly Expr expression = expression;
    }
    //< stmt-print
    //> stmt-return
    public class Return(Token keyword, Expr value) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitReturnStmt(this);
        }

        public readonly Token keyword = keyword;
        public readonly Expr value = value;
    }
    //< stmt-return
    //> stmt-var
    public class Var(Token name, Expr initializer) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitVarStmt(this);
        }

        public readonly Token name = name;
        public readonly Expr initializer = initializer;
    }
    //< stmt-var
    //> stmt-while
    public class While(Expr condition, Statement body) : Statement
    {
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }

        public readonly Expr condition = condition;
        public readonly Statement body = body;
    }
    //< stmt-while

    public abstract R Accept<R>(IVisitor<R> visitor);
}
//< Appendix II stmt