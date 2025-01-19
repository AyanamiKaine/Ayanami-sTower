
namespace jlox;

public class LoxFunction(Statement.Function declaration) : ILoxCallable
{
    private readonly Statement.Function _declaration = declaration;

    public int Arity()
    {
        throw new NotImplementedException();
    }

    public dynamic Call(Interpreter interpreter, List<dynamic> arguments)
    {
        throw new NotImplementedException();
    }
}