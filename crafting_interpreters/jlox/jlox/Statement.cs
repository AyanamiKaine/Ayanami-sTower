//> Appendix II stmt
namespace jlox;

using System.Collections.Generic;

public abstract class Statement
{
    internal interface IVisitor<R>
    {
        R VisitBlockStmt(Block stmt);
        R VisitClassStmt(Class stmt);
        R VisitExpressionStmt(Expression stmt);
        R VisitFunctionStmt(Function stmt);
        R VisitIfStmt(If stmt);
        R VisitPrintStmt(Print stmt);
        R VisitReturnStmt(Return stmt);
        R VisitVarStmt(Var stmt);
        R VisitWhileStmt(While stmt);
    }

    // Nested Stmt classes here...
    //> stmt-block
    internal class Block : Statement
    {
        internal Block(List<Statement> statements)
        {
            this.statements = statements;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }

        internal readonly List<Statement> statements;
    }
    //< stmt-block
    //> stmt-class
    internal class Class : Statement
    {
        internal Class(Token name,
                     Expr.Variable superclass,
                     List<Statement.Function> methods)
        {
            this.name = name;
            this.superclass = superclass;
            this.methods = methods;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitClassStmt(this);
        }

        internal readonly Token name;
        internal readonly Expr.Variable superclass;
        internal readonly List<Statement.Function> methods;
    }
    //< stmt-class
    //> stmt-expression
    internal class Expression : Statement
    {
        internal Expression(Expr expression)
        {
            this.expression = expression;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }

        internal readonly Expr expression;
    }
    //< stmt-expression
    //> stmt-function
    internal class Function : Statement
    {
        internal Function(Token name, List<Token> parameters, List<Statement> body)
        {
            this.name = name;
            this.params_ = parameters; // Renamed to avoid conflict with 'params' keyword
            this.body = body;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitFunctionStmt(this);
        }

        internal readonly Token name;
        internal readonly List<Token> params_; // Renamed field
        internal readonly List<Statement> body;
    }
    //< stmt-function
    //> stmt-if
    internal class If : Statement
    {
        internal If(Expr condition, Statement thenBranch, Statement elseBranch)
        {
            this.condition = condition;
            this.thenBranch = thenBranch;
            this.elseBranch = elseBranch;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitIfStmt(this);
        }

        internal readonly Expr condition;
        internal readonly Statement thenBranch;
        internal readonly Statement elseBranch;
    }
    //< stmt-if
    //> stmt-print
    internal class Print : Statement
    {
        internal Print(Expr expression)
        {
            this.expression = expression;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }

        internal readonly Expr expression;
    }
    //< stmt-print
    //> stmt-return
    internal class Return : Statement
    {
        internal Return(Token keyword, Expr value)
        {
            this.keyword = keyword;
            this.value = value;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitReturnStmt(this);
        }

        internal readonly Token keyword;
        internal readonly Expr value;
    }
    //< stmt-return
    //> stmt-var
    internal class Var : Statement
    {
        internal Var(Token name, Expr initializer)
        {
            this.name = name;
            this.initializer = initializer;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitVarStmt(this);
        }

        internal readonly Token name;
        internal readonly Expr initializer;
    }
    //< stmt-var
    //> stmt-while
    internal class While : Statement
    {
        internal While(Expr condition, Statement body)
        {
            this.condition = condition;
            this.body = body;
        }

        internal override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }

        internal readonly Expr condition;
        internal readonly Statement body;
    }
    //< stmt-while

    internal abstract R Accept<R>(IVisitor<R> visitor);
}
//< Appendix II stmt