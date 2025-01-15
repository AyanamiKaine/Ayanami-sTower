namespace jlox;

class Binary(IExpression left, Token oper, IExpression right) : IExpression
{
    public IExpression? Left { get; set; } = left;
    public Token? Operator { get; set; } = oper;
    public IExpression? Right { get; set; } = right;
}