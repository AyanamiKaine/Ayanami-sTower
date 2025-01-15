namespace jlox;

public interface IExpression
{
    IExpression? Left { get; set; }
    Token? Operator { get; set; }
    IExpression? Right { get; set; }
}

