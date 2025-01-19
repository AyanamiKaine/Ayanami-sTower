

namespace jlox;

public class LoxFunction(Statement.Function declaration, LoxEnvironment closure) : ILoxCallable
{
    private readonly Statement.Function _declaration = declaration;
    private readonly LoxEnvironment _closure = closure;
    public int Arity()
    {
        return _declaration.params_.Count;
    }

    public dynamic? Call(Interpreter interpreter, List<dynamic> arguments)
    {
        LoxEnvironment environment = new(_closure);
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

    public LoxFunction Bind(LoxInstance instance)
    {
        LoxEnvironment environment = new(_closure);
        environment.Define("this", instance);
        return new LoxFunction(_declaration, environment);
    }
}