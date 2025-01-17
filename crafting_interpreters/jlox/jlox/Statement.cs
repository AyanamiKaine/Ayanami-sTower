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
    public class Block : Statement
    {
        public Block(List<Statement> statements)
        {
            this.statements = statements;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }

        public readonly List<Statement> statements;
    }
    //< stmt-block
    //> stmt-class
    public class Class : Statement
    {
        public Class(Token name,
                     Expr.Variable superclass,
                     List<Statement.Function> methods)
        {
            this.name = name;
            this.superclass = superclass;
            this.methods = methods;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitClassStmt(this);
        }

        public readonly Token name;
        public readonly Expr.Variable superclass;
        public readonly List<Statement.Function> methods;
    }
    //< stmt-class
    //> stmt-expression
    public class Expression : Statement
    {
        public Expression(Expr expression)
        {
            this.expression = expression;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }

        public readonly Expr expression;
    }
    //< stmt-expression
    //> stmt-function
    public class Function : Statement
    {
        public Function(Token name, List<Token> parameters, List<Statement> body)
        {
            this.name = name;
            this.params_ = parameters; // Renamed to avoid conflict with 'params' keyword
            this.body = body;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitFunctionStmt(this);
        }

        public readonly Token name;
        public readonly List<Token> params_; // Renamed field
        public readonly List<Statement> body;
    }
    //< stmt-function
    //> stmt-if
    public class If : Statement
    {
        public If(Expr condition, Statement thenBranch, Statement elseBranch)
        {
            this.condition = condition;
            this.thenBranch = thenBranch;
            this.elseBranch = elseBranch;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitIfStmt(this);
        }

        public readonly Expr condition;
        public readonly Statement thenBranch;
        public readonly Statement elseBranch;
    }
    //< stmt-if
    //> stmt-print
    public class Print : Statement
    {
        public Print(Expr expression)
        {
            this.expression = expression;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }

        public readonly Expr expression;
    }
    //< stmt-print
    //> stmt-return
    public class Return : Statement
    {
        public Return(Token keyword, Expr value)
        {
            this.keyword = keyword;
            this.value = value;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitReturnStmt(this);
        }

        public readonly Token keyword;
        public readonly Expr value;
    }
    //< stmt-return
    //> stmt-var
    public class Var : Statement
    {
        public Var(Token name, Expr initializer)
        {
            this.name = name;
            this.initializer = initializer;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitVarStmt(this);
        }

        public readonly Token name;
        public readonly Expr initializer;
    }
    //< stmt-var
    //> stmt-while
    public class While : Statement
    {
        public While(Expr condition, Statement body)
        {
            this.condition = condition;
            this.body = body;
        }

        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }

        public readonly Expr condition;
        public readonly Statement body;
    }
    //< stmt-while

    public abstract R Accept<R>(IVisitor<R> visitor);
}
//< Appendix II stmt