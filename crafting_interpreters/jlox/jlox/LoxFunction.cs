
namespace jlox;

public class LoxFunction(Statement.Function declaration) : ILoxCallable
{
    private readonly Statement.Function _declaration = declaration;

    public int Arity()
    {
        return _declaration.params_.Count;
    }

    public dynamic? Call(Interpreter interpreter, List<dynamic> arguments)
    {
        LoxEnvironment environment = new(interpreter.Globals);
        for (int i = 0; i < _declaration.params_.Count; i++)
        {
            environment.Define(_declaration.params_[i].Lexeme, arguments[i]);
        }
        try
        {
            interpreter.ExecuteBlock(_declaration.body, environment);
        }
        catch (Return returnValue)
        {
            return returnValue.Value;
        }
        return null;
    }

    public override string ToString()
    {
        return $"<fn {_declaration.name.Lexeme}>";
    }
}